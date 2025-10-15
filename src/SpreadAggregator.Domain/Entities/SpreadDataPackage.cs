using System.Collections.Generic;

namespace SpreadAggregator.Domain.Entities;

public class SpreadDataPackage
{
    public List<string> Fields { get; set; } = new List<string> { "exchange", "symbol", "bestBid", "bestAsk", "spreadPercentage", "minVolume", "maxVolume" };
    public List<List<object>> Data { get; set; } = new List<List<object>>();
}