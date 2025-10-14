using Binance.Net.Clients;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services.Exchanges;

public class BinanceExchangeClient : IExchangeClient
{
    public string ExchangeName => "Binance";
    private readonly BinanceSocketClient _socketClient;
    private readonly BinanceRestClient _restClient;

    public BinanceExchangeClient()
    {
        _socketClient = new BinanceSocketClient();
        _restClient = new BinanceRestClient();
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
            QuoteVolume = t.QuoteVolume
        });
    }

    public async Task SubscribeToTickersAsync(IEnumerable<string> symbols, Action<SpreadData> onData)
    {
        var symbolsList = symbols.ToList();
        const int batchSize = 100; // Binance allows up to 200 streams per connection, 100 is a safe number.

        for (int i = 0; i < symbolsList.Count; i += batchSize)
        {
            var batch = symbolsList.Skip(i).Take(batchSize).ToList();
            Console.WriteLine($"[BinanceExchangeClient] Subscribing to batch {i / batchSize + 1}, containing {batch.Count} symbols.");

            var result = await _socketClient.SpotApi.ExchangeData.SubscribeToBookTickerUpdatesAsync(batch, data =>
            {
                onData(new SpreadData
                {
                    Exchange = "Binance",
                    Symbol = data.Data.Symbol,
                    BestBid = data.Data.BestBidPrice,
                    BestAsk = data.Data.BestAskPrice
                });
            });

            if (!result.Success)
            {
                Console.WriteLine($"[ERROR] Failed to subscribe to batch {i / batchSize + 1}: {result.Error}");
            }
            else
            {
                Console.WriteLine($"[BinanceExchangeClient] Successfully subscribed to batch {i / batchSize + 1}.");
                result.Data.ConnectionLost += () => Console.WriteLine($"[BinanceExchangeClient] Connection lost for batch {i / batchSize + 1}.");
                result.Data.ConnectionRestored += (t) => Console.WriteLine($"[BinanceExchangeClient] Connection restored for batch {i / batchSize + 1} after {t}.");
            }
        }
    }

    public IExchangeClient? GetClient(string name)
    {
        return string.Equals(name, ExchangeName, StringComparison.OrdinalIgnoreCase) ? this : null;
    }
}