namespace Tms.Orders.Application.Abstractions;

/// <summary>
/// Abstraction for invalidating cached OrderSnapshot.
/// Implemented in Infrastructure (Redis) — Application layer stays framework-agnostic.
/// </summary>
public interface IOrderCacheInvalidator
{
    Task InvalidateAsync(Guid orderId, CancellationToken ct = default);
}

/// <summary>No-op implementation — used in tests and when cache is not configured.</summary>
public sealed class NullOrderCacheInvalidator : IOrderCacheInvalidator
{
    public static readonly NullOrderCacheInvalidator Instance = new();
    public Task InvalidateAsync(Guid orderId, CancellationToken ct = default) => Task.CompletedTask;
}
