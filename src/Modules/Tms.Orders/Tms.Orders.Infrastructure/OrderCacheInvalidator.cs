using Microsoft.Extensions.Caching.Distributed;
using Tms.Orders.Application;

namespace Tms.Orders.Infrastructure;

public sealed class OrderCacheInvalidator(IDistributedCache cache) : IOrderCacheInvalidator
{
    public Task InvalidateAsync(Guid orderId, CancellationToken ct = default)
        => CachedOrderQueryService.InvalidateAsync(cache, orderId, ct);
}
