using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tms.SharedKernel.Infrastructure;


namespace Tms.SharedKernel.Application;

/// <summary>
/// MediatR pipeline behavior that de-duplicates IIdempotentCommand requests.
/// Uses a PostgreSQL table (IdempotencyDbContext) to track processed command keys.
/// On duplicate: returns the cached result without re-executing the handler.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IdempotencyDbContext db,
    ITenantContext tenantContext,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var key = $"{tenantContext.TenantId}:{request.IdempotencyKey}";

        // ── Check for existing processed record ──────────────────────────
        var existing = await db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == key, cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation(
                "Idempotency: returning cached result for key {Key} (processed {At})",
                key, existing.ProcessedAt);

            if (existing.ResultJson is not null && typeof(TResponse) != typeof(Unit))
            {
                var cached = JsonSerializer.Deserialize<TResponse>(existing.ResultJson);
                if (cached is not null) return cached;
            }

            // void commands (TResponse = Unit) — just return Unit
            return default!;
        }

        // ── Execute the command ────────────────────────────────────────────
        var result = await next();

        // ── Persist idempotency record ─────────────────────────────────────
        try
        {
            var record = new IdempotencyRecord
            {
                IdempotencyKey = key,
                CommandType    = typeof(TRequest).FullName ?? typeof(TRequest).Name,
                ResultJson     = typeof(TResponse) != typeof(Unit)
                                 ? JsonSerializer.Serialize(result)
                                 : null,
                TenantId       = tenantContext.TenantId
            };
            db.IdempotencyRecords.Add(record);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Race condition: another request with same key won — safe to ignore
            logger.LogWarning(ex, "Idempotency record insert conflict for key {Key} — ignoring", key);
        }

        return result;
    }
}
