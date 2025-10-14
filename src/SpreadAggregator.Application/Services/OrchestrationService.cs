using Microsoft.Extensions.Configuration;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Domain.Services;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpreadAggregator.Application.Services;

public class OrchestrationService
{
    private readonly IExchangeClient _exchangeClient;
    private readonly IWebSocketServer _webSocketServer;
    private readonly SpreadCalculator _spreadCalculator;
    private readonly VolumeFilter _volumeFilter;
    private readonly IConfiguration _configuration;

    public OrchestrationService(
        IExchangeClient exchangeClient,
        IWebSocketServer webSocketServer,
        SpreadCalculator spreadCalculator,
        IConfiguration configuration,
        VolumeFilter volumeFilter)
    {
        _exchangeClient = exchangeClient;
        _webSocketServer = webSocketServer;
        _spreadCalculator = spreadCalculator;
        _configuration = configuration;
        _volumeFilter = volumeFilter;
    }

    public async Task StartAsync()
    {
        _webSocketServer.Start();

        var enabledExchanges = _configuration.GetSection("ExchangeSettings:EnabledExchanges").Get<string[]>();
        if (enabledExchanges == null || !enabledExchanges.Any())
            return;

        var tickers = (await _exchangeClient.GetTickersAsync()).ToList();
        Console.WriteLine($"[OrchestrationService] Received {tickers.Count} tickers from the exchange.");

        var filteredSymbols = tickers
            .Where(t => _volumeFilter.IsVolumeSufficient(t.QuoteVolume))
            .Select(t => t.Symbol)
            .ToList();
        Console.WriteLine($"[OrchestrationService] {filteredSymbols.Count} symbols passed the volume filter.");

        if (!filteredSymbols.Any())
        {
            Console.WriteLine("[OrchestrationService] No symbols to subscribe to after filtering. The application will wait for new data.");
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