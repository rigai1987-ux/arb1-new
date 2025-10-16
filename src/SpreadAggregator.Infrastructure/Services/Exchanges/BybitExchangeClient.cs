using Bybit.Net.Clients;
using Bybit.Net.Interfaces.Clients;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services.Exchanges;

public class BybitExchangeClient : IExchangeClient
{
    public string ExchangeName => "Bybit";
    private readonly IBybitSocketClient _socketClient;
    private readonly IBybitRestClient _restClient;

    public BybitExchangeClient(IBybitSocketClient socketClient, IBybitRestClient restClient)
    {
        _socketClient = socketClient;
        _restClient = restClient;
    }

    public async Task<IEnumerable<string>> GetSymbolsAsync()
    {
        var symbols = await _restClient.V5Api.ExchangeData.GetSpotSymbolsAsync();
        return symbols.Data.List.Select(s => s.Name);
    }

    public async Task<IEnumerable<TickerData>> GetTickersAsync()
    {
        var tickers = await _restClient.V5Api.ExchangeData.GetSpotTickersAsync();
        return tickers.Data.List.Select(t => new TickerData
        {
            Symbol = t.Symbol,
            QuoteVolume = t.Turnover24h
        });
    }

    public async Task SubscribeToTickersAsync(IEnumerable<string> symbols, Action<SpreadData> onData)
    {
        var symbolsList = symbols.ToList();
        for (int i = 0; i < symbolsList.Count; i += 10)
        {
            var batch = symbolsList.Skip(i).Take(10);
            var result = await _socketClient.V5SpotApi.SubscribeToOrderbookUpdatesAsync(batch, 1, data =>
            {
                var bestBid = data.Data.Bids.FirstOrDefault();
                var bestAsk = data.Data.Asks.FirstOrDefault();

                if (bestBid != null && bestAsk != null)
                {
                    onData(new SpreadData
                    {
                        Exchange = ExchangeName,
                        Symbol = data.Data.Symbol,
                        BestBid = bestBid.Price,
                        BestAsk = bestAsk.Price
                    });
                }
            });

            if (!result.Success)
            {
                Console.WriteLine($"[ERROR] [BybitExchangeClient] Failed to subscribe to batch starting with {batch.FirstOrDefault()}: {result.Error}");
            }
        }
    }
}