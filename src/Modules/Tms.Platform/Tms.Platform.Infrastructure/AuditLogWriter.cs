using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Interfaces;
using Tms.SharedKernel.Application;

namespace Tms.Platform.Infrastructure;

/// <summary>
/// Bridges IAuditLogWriter (SharedKernel) → IAuditLogRepository (Platform).
/// Registered in PlatformModule so the AuditLogBehavior pipeline can work.
/// </summary>
public sealed class AuditLogWriter(IAuditLogRepository repo) : IAuditLogWriter
{
    public async Task WriteAsync(
        string action, string resource, string? resourceId,
        string? details, CancellationToken ct)
    {
        var log = AuditLog.Create(
            action: action,
            resource: resource,
            tenantId: Guid.Empty, // Phase 3: resolve from ICurrentUserContext
            resourceId: resourceId,
            details: details);

        await repo.AddAsync(log, ct);
    }
}
