using Microsoft.Extensions.Caching.Distributed;
using Tms.Orders.Application.Abstractions;
using Tms.Orders.Infrastructure.ReadServices;

namespace Tms.Orders.Infrastructure.Cache;

public sealed class OrderCacheInvalidator(IDistributedCache cache) : IOrderCacheInvalidator
{
    public Task InvalidateAsync(Guid orderId, CancellationToken ct = default)
        => CachedOrderQueryService.InvalidateAsync(cache, orderId, ct);
}
