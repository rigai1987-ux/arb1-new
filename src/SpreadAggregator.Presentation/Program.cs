using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpreadAggregator.Application.Abstractions;
using SpreadAggregator.Application.Services;
using SpreadAggregator.Domain.Services;
using SpreadAggregator.Infrastructure.Services;
using SpreadAggregator.Infrastructure.Services.Exchanges;
using System;
using System.Threading.Tasks;
using BingX.Net.Interfaces.Clients;
using BingX.Net.Clients;

namespace SpreadAggregator.Presentation;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var orchestrationService = host.Services.GetRequiredService<OrchestrationService>();
        await orchestrationService.StartAsync();

        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.Sources.Clear();
                var env = hostingContext.HostingEnvironment;
                configuration
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IWebSocketServer>(sp =>
                {
                    var connectionString = context.Configuration.GetSection("ConnectionStrings")?["WebSocket"];
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("WebSocket connection string is not configured.");
                    }
                    return new FleckWebSocketServer(connectionString);
                });

                services.AddSingleton<SpreadCalculator>();
                services.AddSingleton<VolumeFilter>();
                services.AddSingleton<SpreadDataCache>();

                // Register all exchange clients
                services.AddSingleton<IExchangeClient, BinanceExchangeClient>();
                services.AddSingleton<IExchangeClient, MexcExchangeClient>();
                services.AddSingleton<IExchangeClient, GateIoExchangeClient>();
                services.AddSingleton<IExchangeClient, KucoinExchangeClient>();
                services.AddSingleton<IExchangeClient, OkxExchangeClient>();
                services.AddSingleton<IExchangeClient, BitgetExchangeClient>();
                services.AddSingleton<IExchangeClient, BingXExchangeClient>();

                services.AddBingX();

                services.AddSingleton<OrchestrationService>();
            });
}
