using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Infrastructure.Services.Exchanges;
using System;
using System.Collections.Generic;

namespace SpreadAggregator.Infrastructure.Services;

public static class ExchangeFactory
{
    public static IReadOnlyDictionary<string, Func<IExchangeClient>> GetExchanges()
    {
        return new Dictionary<string, Func<IExchangeClient>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Binance", () => new BinanceExchangeClient() }
        };
    }
}