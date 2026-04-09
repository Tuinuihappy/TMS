using System.Text.Json;
using MediatR;

namespace Tms.SharedKernel.Application;

/// <summary>
/// Abstraction for audit log writing — implemented by Platform module.
/// Lives in SharedKernel to be used by the pipeline without circular dependencies.
/// </summary>
public interface IAuditLogWriter
{
    Task WriteAsync(
        string action, string resource, string? resourceId,
        string? details, Guid? userId, Guid? tenantId,
        CancellationToken ct);
}

/// <summary>
/// MediatR Pipeline Behavior ที่บันทึก Audit Log อัตโนมัติ
/// สำหรับ Command ที่ implement IAuditableCommand
/// รวม UserId + TenantId จาก ITenantContext
/// </summary>
public sealed class AuditLogBehavior<TRequest, TResponse>(
    IAuditLogWriter auditWriter,
    ITenantContext tenantContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditableCommand
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var result = await next();

        try
        {
            var details = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                MaxDepth = 3
            });

            if (details.Length > 2000) details = details[..2000];

            await auditWriter.WriteAsync(
                action:     typeof(TRequest).Name,
                resource:   request.ResourceName,
                resourceId: request.ResourceId,
                details:    details,
                userId:     tenantContext.IsAuthenticated ? tenantContext.UserId : null,
                tenantId:   tenantContext.IsAuthenticated ? tenantContext.TenantId : null,
                ct:         cancellationToken);
        }
        catch
        {
            // Audit log failure must never break the main operation
        }

        return result;
    }
}
