using BingX.Net.Clients;
using BingX.Net.Interfaces;
using BingX.Net.Interfaces.Clients;
using CryptoExchange.Net.SharedApis;
using CryptoExchange.Net.Objects;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services.Exchanges;

public class BingXExchangeClient : IExchangeClient
{
    public string ExchangeName => "BingX";
    private readonly IBingXSocketClient _socketClient;
    private readonly IBingXRestClient _restClient;

    public BingXExchangeClient(IBingXSocketClient socketClient, IBingXRestClient restClient)
    {
        _socketClient = socketClient;
        _restClient = restClient;
    }

    public async Task<IEnumerable<string>> GetSymbolsAsync()
    {
        var symbols = await _restClient.SpotApi.ExchangeData.GetSymbolsAsync();
        return symbols.Data.Select(s => s.Name);
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
        foreach (var symbol in symbols)
        {
            var result = await _socketClient.SpotApi.SubscribeToBookPriceUpdatesAsync(symbol, data =>
            {
                onData(new SpreadData
                {
                    Exchange = ExchangeName,
                    Symbol = data.Data.Symbol,
                    BestBid = data.Data.BestBidPrice,
                    BestAsk = data.Data.BestAskPrice
                });
            });

            if (!result.Success)
            {
                Console.WriteLine($"[ERROR] [BingXExchangeClient] Failed to subscribe to {symbol}: {result.Error}");
            }
        }
    }
}