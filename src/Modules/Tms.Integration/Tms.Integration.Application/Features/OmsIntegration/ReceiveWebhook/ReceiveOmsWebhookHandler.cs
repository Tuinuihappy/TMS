using MediatR;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Interfaces;
using Tms.SharedKernel.Application;

namespace Tms.Integration.Application.Features.OmsIntegration.ReceiveWebhook;

public sealed record ReceiveOmsWebhookCommand(
    string OmsProviderCode,
    string RawPayload,
    Guid TenantId
) : ICommand<Guid>;

public sealed class ReceiveOmsWebhookHandler(IOmsSyncRepository repo)
    : ICommandHandler<ReceiveOmsWebhookCommand, Guid>
{
    public async Task<Guid> Handle(ReceiveOmsWebhookCommand request, CancellationToken cancellationToken)
    {
        // Extract external order ref from payload (best-effort — ใช้ key ที่พบก่อน)
        var externalRef = TryExtractExternalRef(request.RawPayload)
            ?? $"UNKNOWN-{Guid.NewGuid():N}";

        // ถ้า ExternalOrderRef ซ้ำ — idempotent return existing sync
        if (await repo.ExistsAsync(externalRef, request.OmsProviderCode, cancellationToken))
            return Guid.Empty; // caller รับ 202 อยู่ดี

        var sync = OmsOrderSync.CreateInbound(
            externalRef,
            request.OmsProviderCode,
            request.RawPayload,
            request.TenantId);

        await repo.AddAsync(sync, cancellationToken);
        return sync.Id;
    }

    private static string? TryExtractExternalRef(string json)
    {
        // Prefer the current mapped field names, then fall back to legacy aliases.
        var candidates = new[] { "externalRef", "external_ref", "order_id", "orderId", "order_ref", "orderRef", "id" };
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            foreach (var key in candidates)
            {
                if (doc.RootElement.TryGetProperty(key, out var prop))
                    return prop.GetString();
            }
        }
        catch { /* ignore parse errors */ }
        return null;
    }
}
