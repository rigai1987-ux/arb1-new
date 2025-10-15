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
    private readonly IBingXOrderBookFactory _orderBookFactory;

    public BingXExchangeClient(IBingXSocketClient socketClient, IBingXRestClient restClient, IBingXOrderBookFactory orderBookFactory)
    {
        _socketClient = socketClient;
        _restClient = restClient;
        _orderBookFactory = orderBookFactory;
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
            var book = _orderBookFactory.Create(new SharedSymbol(TradingMode.Spot, symbol.Split('-')[0], symbol.Split('-')[1]));
            book.OnOrderBookUpdate += (update) =>
            {
                var bestBid = update.Bids.FirstOrDefault();
                var bestAsk = update.Asks.FirstOrDefault();

                if (bestBid != null && bestAsk != null)
                {
                    onData(new SpreadData
                    {
                        Exchange = ExchangeName,
                        Symbol = symbol,
                        BestBid = bestBid.Price,
                        BestAsk = bestAsk.Price
                    });
                }
            };

            var result = await book.StartAsync();
            if (!result.Success)
            {
                Console.WriteLine($"[ERROR] [BingXExchangeClient] Failed to subscribe to {symbol}: {result.Error}");
            }
        }
    }
}