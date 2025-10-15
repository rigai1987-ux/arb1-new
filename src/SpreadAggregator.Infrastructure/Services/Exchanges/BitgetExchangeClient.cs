using Bitget.Net.Clients;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services.Exchanges;

public class BitgetExchangeClient : IExchangeClient
{
    public string ExchangeName => "Bitget";
    private readonly BitgetSocketClient _socketClient;
    private readonly BitgetRestClient _restClient;

    public BitgetExchangeClient()
    {
        _socketClient = new BitgetSocketClient();
        _restClient = new BitgetRestClient();
    }

    public async Task<IEnumerable<string>> GetSymbolsAsync()
    {
        var symbols = await _restClient.SpotApiV2.ExchangeData.GetSymbolsAsync();
        return symbols.Data.Select(s => s.Symbol);
    }

    public async Task<IEnumerable<TickerData>> GetTickersAsync()
    {
        var tickers = await _restClient.SpotApiV2.ExchangeData.GetTickersAsync();
        return tickers.Data.Select(t => new TickerData
        {
            Symbol = t.Symbol,
            QuoteVolume = t.QuoteVolume
        });
    }

    public async Task SubscribeToTickersAsync(IEnumerable<string> symbols, Action<SpreadData> onData)
    {
        var result = await _socketClient.SpotApiV2.SubscribeToOrderBookUpdatesAsync(symbols, 1, data =>
        {
            var update = data.Data;
            var bestBid = update.Bids.FirstOrDefault();
            var bestAsk = update.Asks.FirstOrDefault();

            if (bestBid != null && bestAsk != null)
            {
                onData(new SpreadData
                {
                    Exchange = ExchangeName,
                    Symbol = data.Symbol,
                    BestBid = bestBid.Price,
                    BestAsk = bestAsk.Price
                });
            }
        });

        if (!result.Success)
        {
            Console.WriteLine($"[ERROR] [BitgetExchangeClient] Failed to subscribe: {result.Error}");
        }
        else
        {
            Console.WriteLine($"[BitgetExchangeClient] Successfully subscribed to {symbols.Count()} symbols.");
            result.Data.ConnectionLost += () => Console.WriteLine($"[BitgetExchangeClient] Connection lost.");
            result.Data.ConnectionRestored += (t) => Console.WriteLine($"[BitgetExchangeClient] Connection restored after {t}.");
        }
    }
}