using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;
using Tms.SharedKernel.Application;

namespace Tms.Platform.Infrastructure;

/// <summary>
/// Bridges IAuditLogWriter (SharedKernel) → IAuditLogRepository (Platform).
/// Captures UserId + TenantId from the pipeline for every auditable command.
/// </summary>
public sealed class AuditLogWriter(IAuditLogRepository repo) : IAuditLogWriter
{
    public async Task WriteAsync(
        string action, string resource, string? resourceId,
        string? details, Guid? userId, Guid? tenantId,
        CancellationToken ct)
    {
        var log = AuditLog.Create(
            action:     action,
            resource:   resource,
            tenantId:   tenantId ?? Guid.Empty,
            userId:     userId,
            resourceId: resourceId,
            details:    details);

        await repo.AddAsync(log, ct);
    }
}
