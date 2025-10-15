using OKX.Net.Clients;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services.Exchanges;

public class OkxExchangeClient : IExchangeClient
{
    public string ExchangeName => "OKX";
    private readonly OKXSocketClient _socketClient;
    private readonly OKXRestClient _restClient;

    public OkxExchangeClient()
    {
        _socketClient = new OKXSocketClient();
        _restClient = new OKXRestClient();
    }

    public async Task<IEnumerable<string>> GetSymbolsAsync()
    {
        var tickers = await _restClient.UnifiedApi.ExchangeData.GetTickersAsync(OKX.Net.Enums.InstrumentType.Spot);
        return tickers.Data.Select(t => t.Symbol);
    }

    public async Task<IEnumerable<TickerData>> GetTickersAsync()
    {
        var tickers = await _restClient.UnifiedApi.ExchangeData.GetTickersAsync(OKX.Net.Enums.InstrumentType.Spot);
        return tickers.Data.Select(t => new TickerData
        {
            Symbol = t.Symbol,
            QuoteVolume = t.QuoteVolume
        });
    }

    public async Task SubscribeToTickersAsync(IEnumerable<string> symbols, Action<SpreadData> onData)
    {
        var symbolsList = symbols.ToList();
        // OKX has a limit of 100 subscriptions per connection.
        const int maxSubscriptions = 100;
        if (symbolsList.Count > maxSubscriptions)
        {
            Console.WriteLine($"[WARNING] [OkxExchangeClient] Attempted to subscribe to {symbolsList.Count} symbols, but the limit is {maxSubscriptions}. Subscribing to the first {maxSubscriptions} symbols only.");
            symbolsList = symbolsList.Take(maxSubscriptions).ToList();
        }

        var result = await _socketClient.UnifiedApi.ExchangeData.SubscribeToTickerUpdatesAsync(symbolsList, data =>
        {
            var ticker = data.Data;
            if (ticker.BestBidPrice.HasValue && ticker.BestAskPrice.HasValue)
            {
                onData(new SpreadData
                {
                    Exchange = ExchangeName,
                    Symbol = ticker.Symbol,
                    BestBid = ticker.BestBidPrice.Value,
                    BestAsk = ticker.BestAskPrice.Value
                });
            }
        });

        if (!result.Success)
        {
            Console.WriteLine($"[ERROR] [OkxExchangeClient] Failed to subscribe: {result.Error}");
        }
        else
        {
            Console.WriteLine($"[OkxExchangeClient] Successfully subscribed to {symbolsList.Count} symbols.");
            result.Data.ConnectionLost += () => Console.WriteLine($"[OkxExchangeClient] Connection lost.");
            result.Data.ConnectionRestored += (t) => Console.WriteLine($"[OkxExchangeClient] Connection restored after {t}.");
        }
    }
}