namespace TmsApi.Application.Interfaces;

/// <summary>Business-level audit writer. Values are serialized and sensitive keys redacted by Infrastructure.</summary>
public interface IAuditService
{
    Task LogAsync(string action, string entityName, string entityId,
        object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default);

    Task LogCreateAsync(string entityName, string entityId, object? newValues, CancellationToken cancellationToken = default);
    Task LogUpdateAsync(string entityName, string entityId, object? oldValues, object? newValues, CancellationToken cancellationToken = default);
    Task LogDeleteAsync(string entityName, string entityId, object? oldValues, CancellationToken cancellationToken = default);
    Task LogApprovalAsync(string entityName, string entityId, object? oldValues, object? newValues, CancellationToken cancellationToken = default);
    Task LogGradeChangeAsync(string entityId, object? oldValues, object? newValues, CancellationToken cancellationToken = default);
    Task LogCertificateIssueAsync(string entityId, object? newValues, CancellationToken cancellationToken = default);
}
