using SpreadAggregator.Domain.Entities;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SpreadAggregator.Application.Services;

/// <summary>
/// A thread-safe cache to store the latest spread data for each symbol from each exchange.
/// </summary>
public class SpreadDataCache
{
    private readonly ConcurrentDictionary<string, SpreadData> _cache = new();

    /// <summary>
    /// Updates or adds the spread data for a given symbol.
    /// The key is a composite of the exchange and symbol to ensure uniqueness.
    /// </summary>
    /// <param name="data">The spread data to cache.</param>
    public void Update(SpreadData data)
    {
        var key = $"{data.Exchange}:{data.Symbol}";
        _cache[key] = data;
    }

    /// <summary>
    /// Retrieves all the current spread data from the cache.
    /// </summary>
    /// <returns>A collection of all spread data currently in the cache.</returns>
    public IEnumerable<SpreadData> GetAll()
    {
        return _cache.Values;
    }
}