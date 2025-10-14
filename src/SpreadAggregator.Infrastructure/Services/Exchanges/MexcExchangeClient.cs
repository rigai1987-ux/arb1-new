using Mexc.Net.Clients;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services.Exchanges;

public class MexcExchangeClient : IExchangeClient
{
    public string ExchangeName => "Mexc";
    private readonly MexcSocketClient _socketClient;
    private readonly MexcRestClient _restClient;

    public MexcExchangeClient()
    {
        _socketClient = new MexcSocketClient();
        _restClient = new MexcRestClient();
    }

    public async Task<IEnumerable<string>> GetSymbolsAsync()
    {
        var tickers = await _restClient.SpotApi.ExchangeData.GetTickersAsync();
        return tickers.Data.Select(t => t.Symbol);
    }

    public async Task<IEnumerable<TickerData>> GetTickersAsync()
    {
        var tickers = await _restClient.SpotApi.ExchangeData.GetTickersAsync();
        return tickers.Data.Select(t => new TickerData
        {
            Symbol = t.Symbol,
            QuoteVolume = t.QuoteVolume ?? 0
        });
    }

    public async Task SubscribeToTickersAsync(IEnumerable<string> symbols, Action<SpreadData> onData)
    {
        // MEXC has a very strict limit on the total number of active subscriptions per connection.
        // The error "Exceeded maximum subscription limit!" indicates this.
        // We will cap the total number of subscriptions to a safe value.
        const int maxTotalSubscriptions = 30;
        
        var symbolsToSubscribe = symbols.Take(maxTotalSubscriptions).ToList();
        if (symbols.Count() > maxTotalSubscriptions)
        {
            Console.WriteLine($"[WARNING] [MexcExchangeClient] Attempted to subscribe to {symbols.Count()} symbols, but the limit is {maxTotalSubscriptions}. Subscribing to the first {maxTotalSubscriptions} symbols only.");
        }

        const int batchSize = 10; // A safe number of symbols per single subscription message.
        const int delayBetweenSubscriptions = 1000; // 1 second delay to avoid rate limiting.

        for (int i = 0; i < symbolsToSubscribe.Count; i += batchSize)
        {
            var batch = symbolsToSubscribe.Skip(i).Take(batchSize).ToList();
            var batchNumber = i / batchSize + 1;
            Console.WriteLine($"[MexcExchangeClient] Subscribing to batch {batchNumber}, containing {batch.Count} symbols.");

            var result = await _socketClient.SpotApi.SubscribeToBookTickerUpdatesAsync(batch, data =>
            {
                onData(new SpreadData
                {
                    Exchange = "Mexc",
                    Symbol = data.Symbol,
                    BestBid = data.Data.BestBidPrice,
                    BestAsk = data.Data.BestAskPrice
                });
            });

            if (!result.Success)
            {
                Console.WriteLine($"[ERROR] Failed to subscribe to batch {batchNumber}: {result.Error}");
            }
            else
            {
                Console.WriteLine($"[MexcExchangeClient] Successfully subscribed to batch {batchNumber}.");
                result.Data.ConnectionLost += () => Console.WriteLine($"[MexcExchangeClient] Connection lost for batch {batchNumber}.");
                result.Data.ConnectionRestored += (t) => Console.WriteLine($"[MexcExchangeClient] Connection restored for batch {batchNumber} after {t}.");
            }

            // Wait before sending the next subscription request.
            await Task.Delay(delayBetweenSubscriptions);
        }
    }
}