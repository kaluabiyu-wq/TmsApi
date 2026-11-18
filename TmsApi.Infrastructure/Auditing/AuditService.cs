using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Auditing;

public sealed class AuditService(TmsDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "token", "accesstoken", "refreshtoken", "authorization",
        "secret", "clientsecret", "apikey", "privatekey", "securitystamp", "concurrencyStamp",
        "cookie", "set-cookie", "credential", "credentials"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LogAsync(string action, string entityName, string entityId,
        object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("Audit action, entity name and entity id are required.");

        var context = httpContextAccessor.HttpContext;
        var userId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context?.User.FindFirstValue("sub") ?? "system";
        var correlationId = context?.Items[RequestLoggingMiddlewareKeys.CorrelationId]?.ToString()
                            ?? context?.TraceIdentifier;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action.Trim(),
            EntityName = entityName.Trim(),
            EntityId = entityId.Trim(),
            OldValues = SafeJson(oldValues),
            NewValues = SafeJson(newValues),
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task LogCreateAsync(string entityName, string entityId, object? newValues, CancellationToken ct = default) =>
        LogAsync($"{entityName}Created", entityName, entityId, null, newValues, ct);
    public Task LogUpdateAsync(string entityName, string entityId, object? oldValues, object? newValues, CancellationToken ct = default) =>
        LogAsync($"{entityName}Updated", entityName, entityId, oldValues, newValues, ct);
    public Task LogDeleteAsync(string entityName, string entityId, object? oldValues, CancellationToken ct = default) =>
        LogAsync($"{entityName}Deleted", entityName, entityId, oldValues, null, ct);
    public Task LogApprovalAsync(string entityName, string entityId, object? oldValues, object? newValues, CancellationToken ct = default) =>
        LogAsync($"{entityName}Approved", entityName, entityId, oldValues, newValues, ct);
    public Task LogGradeChangeAsync(string entityId, object? oldValues, object? newValues, CancellationToken ct = default) =>
        LogAsync("GradeUpdated", "Grade", entityId, oldValues, newValues, ct);
    public Task LogCertificateIssueAsync(string entityId, object? newValues, CancellationToken ct = default) =>
        LogAsync("CertificateIssued", "Certificate", entityId, null, newValues, ct);

    private static string? SafeJson(object? value)
    {
        if (value is null) return null;
        var json = JsonSerializer.SerializeToNode(value, JsonOptions);
        Redact(json);
        return json?.ToJsonString(JsonOptions);
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToArray())
            {
                if (SensitiveNames.Contains(key) || key.Contains("token", StringComparison.OrdinalIgnoreCase) || key.Contains("password", StringComparison.OrdinalIgnoreCase))
                    obj[key] = "[REDACTED]";
                else Redact(obj[key]);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) Redact(item);
        }
    }
}

/// <summary>Shared HttpContext item key avoids a dependency from Infrastructure onto the API project.</summary>
public static class RequestLoggingMiddlewareKeys
{
    public const string CorrelationId = "Tms.CorrelationId";
}
