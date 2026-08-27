using Microsoft.Extensions.Caching.Memory;
using SystemOrder.Application.Interfaces;
using SystemOrder.Domain.Entities;

namespace SystemOrder.Infrastructure.Cache;

public class OrderMemoryCache : IOrderCache
{
    private const string CacheKey = "orders";

    private readonly IMemoryCache _memoryCache;

    public OrderMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public IEnumerable<Order>? Get()
    {
        return _memoryCache.Get<IEnumerable<Order>>(CacheKey);
    }

    public void Set(IEnumerable<Order> orders)
    {
        _memoryCache.Set(
            CacheKey,
            orders.ToList(),
            TimeSpan.FromMinutes(5));
    }

    public void Remove()
    {
        _memoryCache.Remove(CacheKey);
    }
}