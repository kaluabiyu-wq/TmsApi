using System.Globalization;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/audit")]
[Authorize(Policy = "ViewAuditLogs")]
public sealed class AuditController(TmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? userId, [FromQuery] string? action,
        [FromQuery] string? entityName, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = Filter(db.AuditLogs.AsNoTracking(), userId, action, entityName, from, to);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto()).ToListAsync(ct);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> ByUser(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        await Get(userId: id, action: null, entityName: null, from: null, to: null, page: page, pageSize: pageSize, ct: ct);

    [HttpGet("entities/{type}/{id}")]
    public async Task<IActionResult> ByEntity(string type, string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking().Where(x => x.EntityName == type && x.EntityId == id);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto()).ToListAsync(ct);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? userId, [FromQuery] string? action,
        [FromQuery] string? entityName, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var rows = await Filter(db.AuditLogs.AsNoTracking(), userId, action, entityName, from, to)
            .OrderByDescending(x => x.Timestamp).Take(10000).ToListAsync(ct);
        var csv = new StringBuilder("Id,TimestampUtc,UserId,Action,EntityName,EntityId,OldValues,NewValues,IpAddress,CorrelationId\r\n");
        foreach (var x in rows)
            csv.AppendJoin(',', new[] { x.Id.ToString(CultureInfo.InvariantCulture), x.Timestamp.ToString("O"), x.UserId,
                x.Action, x.EntityName, x.EntityId, x.OldValues, x.NewValues, x.IpAddress, x.CorrelationId }.Select(Csv)).AppendLine();
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"tms-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static IQueryable<AuditLog> Filter(IQueryable<AuditLog> q, string? userId, string? action,
        string? entityName, DateTime? from, DateTime? to)
    {
        if (!string.IsNullOrWhiteSpace(userId)) q = q.Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => x.Action.ToLower().Contains(action.ToLower()));
        if (!string.IsNullOrWhiteSpace(entityName)) q = q.Where(x => x.EntityName.ToLower() == entityName.ToLower());
        if (from.HasValue) q = q.Where(x => x.Timestamp >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
        if (to.HasValue) q = q.Where(x => x.Timestamp <= DateTime.SpecifyKind(to.Value, DateTimeKind.Utc));
        return q;
    }

    private static System.Linq.Expressions.Expression<Func<AuditLog, AuditLogDto>> ToDto() => x => new AuditLogDto(
        x.Id, x.UserId, x.Action, x.EntityName, x.EntityId, x.OldValues, x.NewValues, x.IpAddress, x.Timestamp, x.CorrelationId);

    private static string Csv(string? value)
    {
        value ??= "";
        // Prevent spreadsheet formula injection in exported text fields.
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

public sealed record AuditLogDto(long Id, string UserId, string Action, string EntityName, string EntityId,
    string? OldValues, string? NewValues, string? IpAddress, DateTime Timestamp, string? CorrelationId);
