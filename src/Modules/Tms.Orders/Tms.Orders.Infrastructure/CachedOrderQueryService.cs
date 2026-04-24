using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Infrastructure;

/// <summary>
/// Decorator สำหรับ IOrderQueryService — เพิ่ม Redis cache layer
/// Planning module เรียก GetOrderAsync/GetOrdersByIdsAsync บ่อยมากใน auto-planning
/// Cache ช่วยลด DB round-trip จาก Planning → Orders schema
/// </summary>
public sealed class CachedOrderQueryService(
    OrderQueryService inner,
    IDistributedCache cache) : IOrderQueryService
{
    private static readonly TimeSpan TTL = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public static string CacheKey(Guid orderId) => $"order:snapshot:{orderId}";

    public async Task<OrderSnapshot?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var key = CacheKey(orderId);
        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<OrderSnapshot>(cached, JsonOpts);

        var snapshot = await inner.GetOrderAsync(orderId, ct);
        if (snapshot is not null)
            await cache.SetStringAsync(key,
                JsonSerializer.Serialize(snapshot, JsonOpts),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TTL },
                ct);

        return snapshot;
    }

    public async Task<List<OrderSnapshot>> GetOrdersByIdsAsync(
        IEnumerable<Guid> orderIds, CancellationToken ct = default)
    {
        var ids = orderIds.ToList();
        var results = new Dictionary<Guid, OrderSnapshot>(ids.Count);
        var missedIds = new List<Guid>(ids.Count);

        // Try cache per ID
        foreach (var id in ids)
        {
            var cached = await cache.GetStringAsync(CacheKey(id), ct);
            if (cached is not null)
                results[id] = JsonSerializer.Deserialize<OrderSnapshot>(cached, JsonOpts)!;
            else
                missedIds.Add(id);
        }

        // Batch fetch missed IDs from DB
        if (missedIds.Count > 0)
        {
            var fetched = await inner.GetOrdersByIdsAsync(missedIds, ct);
            foreach (var snapshot in fetched)
            {
                results[snapshot.Id] = snapshot;
                await cache.SetStringAsync(
                    CacheKey(snapshot.Id),
                    JsonSerializer.Serialize(snapshot, JsonOpts),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TTL },
                    ct);
            }
        }

        // Preserve original order
        return ids
            .Where(id => results.ContainsKey(id))
            .Select(id => results[id])
            .ToList();
    }

    /// <summary>Invalidates cached snapshot — call after any Order mutation.</summary>
    public static Task InvalidateAsync(IDistributedCache cache, Guid orderId, CancellationToken ct = default)
        => cache.RemoveAsync(CacheKey(orderId), ct);
}
