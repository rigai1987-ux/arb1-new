using Microsoft.Extensions.Configuration;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpreadAggregator.Application.Services;

public class OrchestrationService
{
    private readonly IWebSocketServer _webSocketServer;
    private readonly SpreadCalculator _spreadCalculator;
    private readonly VolumeFilter _volumeFilter;
    private readonly IConfiguration _configuration;
    private readonly IExchangeClient _exchangeClient;

    public OrchestrationService(
        IWebSocketServer webSocketServer,
        SpreadCalculator spreadCalculator,
        IConfiguration configuration,
        VolumeFilter volumeFilter,
        IExchangeClient exchangeClient)
    {
        _webSocketServer = webSocketServer;
        _spreadCalculator = spreadCalculator;
        _configuration = configuration;
        _volumeFilter = volumeFilter;
        _exchangeClient = exchangeClient;
    }

    public async Task StartAsync()
    {
        _webSocketServer.Start();

        var exchangeName = "Binance"; // Hardcoded for now
        var minVolume = _configuration.GetValue<decimal>($"ExchangeSettings:Exchanges:{exchangeName}:VolumeFilter:MinUsdVolume");
        var maxVolume = _configuration.GetValue<decimal>($"ExchangeSettings:Exchanges:{exchangeName}:VolumeFilter:MaxUsdVolume");

        var tickers = (await _exchangeClient.GetTickersAsync()).ToList();
        Console.WriteLine($"[{exchangeName}] Received {tickers.Count} tickers.");

        var filteredSymbols = tickers
            .Where(t => t.Symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase) && _volumeFilter.IsVolumeSufficient(t.QuoteVolume, minVolume, maxVolume))
            .Select(t => t.Symbol)
            .ToList();
        Console.WriteLine($"[{exchangeName}] {filteredSymbols.Count} symbols passed the volume filter.");

        if (!filteredSymbols.Any())
        {
            Console.WriteLine($"[{exchangeName}] No symbols to subscribe to after filtering.");
            return;
        }

        await _exchangeClient.SubscribeToTickersAsync(filteredSymbols, async spreadData =>
        {
            if (spreadData.BestAsk == 0) return;

            spreadData.SpreadPercentage = _spreadCalculator.Calculate(spreadData.BestBid, spreadData.BestAsk);

            var message = JsonSerializer.Serialize(spreadData);
            await _webSocketServer.BroadcastAsync(message);
        });
    }
}