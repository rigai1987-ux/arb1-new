using Kucoin.Net.Clients;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services.Exchanges;

public class KucoinExchangeClient : IExchangeClient
{
    public string ExchangeName => "Kucoin";
    private readonly KucoinSocketClient _socketClient;
    private readonly KucoinRestClient _restClient;

    public KucoinExchangeClient()
    {
        _socketClient = new KucoinSocketClient();
        _restClient = new KucoinRestClient();
    }

    public async Task<IEnumerable<string>> GetSymbolsAsync()
    {
        var markets = await _restClient.SpotApi.ExchangeData.GetSymbolsAsync();
        return markets.Data.Select(m => m.Symbol);
    }

    public async Task<IEnumerable<TickerData>> GetTickersAsync()
    {
        var tickers = await _restClient.SpotApi.ExchangeData.GetTickersAsync();
        return tickers.Data.Data.Select(t => new TickerData
        {
            Symbol = t.Symbol,
            QuoteVolume = t.QuoteVolume ?? 0
        });
    }

    public async Task SubscribeToTickersAsync(IEnumerable<string> symbols, Action<SpreadData> onData)
    {
        // Kucoin has a limit of 100 subscriptions per connection.
        var symbolsToSubscribe = symbols.ToList();
        const int maxSubscriptions = 100;
        if (symbolsToSubscribe.Count > maxSubscriptions)
        {
            Console.WriteLine($"[WARNING] [KucoinExchangeClient] Attempted to subscribe to {symbolsToSubscribe.Count} symbols, but the limit is {maxSubscriptions}. Subscribing to the first {maxSubscriptions} symbols only.");
            symbolsToSubscribe = symbolsToSubscribe.Take(maxSubscriptions).ToList();
        }

        var result = await _socketClient.SpotApi.SubscribeToBookTickerUpdatesAsync(symbolsToSubscribe, data =>
        {
            onData(new SpreadData
            {
                Exchange = ExchangeName,
                Symbol = data.Symbol,
                BestBid = data.Data.BestBid.Price,
                BestAsk = data.Data.BestAsk.Price
            });
        });

        if (!result.Success)
        {
            Console.WriteLine($"[ERROR] [KucoinExchangeClient] Failed to subscribe: {result.Error}");
        }
        else
        {
            Console.WriteLine($"[KucoinExchangeClient] Successfully subscribed to {symbolsToSubscribe.Count} symbols.");
            result.Data.ConnectionLost += () => Console.WriteLine($"[KucoinExchangeClient] Connection lost.");
            result.Data.ConnectionRestored += (t) => Console.WriteLine($"[KucoinExchangeClient] Connection restored after {t}.");
        }
    }
}