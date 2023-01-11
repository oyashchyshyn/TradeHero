using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Repositories;
using TradeHero.Client.Clients;
using TradeHero.Client.Resolvers;
using TradeHero.Contracts.Client.Resolvers;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.Client;

public static class ThClientServiceCollectionExtensions
{
    public static void AddThClient(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IThRestBinanceClient, ThRestBinanceClient>(serviceProvider =>
        {
            var connectionRepository = serviceProvider.GetRequiredService<IConnectionRepository>();
            var environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var connection = connectionRepository.GetActiveConnection();

            var apiKey = "default";
            var secretKey = "default";
            
            if (connection != null)
            {
                apiKey = connection.ApiKey;
                secretKey = connection.SecretKey;
            }
            
            var restClientOptions = GetBinanceClientOptions(environmentService.GetAppSettings(), apiKey, 
                secretKey, loggerFactory.CreateLogger<ThRestBinanceClient>());
            
            return new ThRestBinanceClient(restClientOptions, serviceProvider);
        });
        
        serviceCollection.AddTransient<IThSocketBinanceClient, ThSocketBinanceClient>(serviceProvider =>
        {
            var connectionRepository = serviceProvider.GetRequiredService<IConnectionRepository>();
            var environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var connection = connectionRepository.GetActiveConnection();
            
            var apiKey = "default";
            var secretKey = "default";
            
            if (connection != null)
            {
                apiKey = connection.ApiKey;
                secretKey = connection.SecretKey;
            }
            
            var socketClientOptions = GetBinanceSocketClientOptions(environmentService.GetAppSettings(), apiKey, 
                secretKey, loggerFactory.CreateLogger<ThSocketBinanceClient>());

            return new ThSocketBinanceClient(socketClientOptions);
        });

        serviceCollection.AddSingleton<IBinanceResolver, BinanceResolver>();
    }

    #region Private methods

    private static BinanceClientOptions GetBinanceClientOptions(AppSettings appSettings, 
        string apiKey, string secretKey, ILogger logger)
    {
        var clientOptions = new BinanceClientOptions
        {
            ApiCredentials = new ApiCredentials(apiKey, secretKey),
            LogLevel = appSettings.Logger.RestClientLogLevel
        };
        
        if (appSettings.Client.Server == ClientServer.TestNet)
        {
            clientOptions = new BinanceClientOptions
            {
                SpotApiOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.RestClientAddress
                },
                UsdFuturesApiOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.UsdFuturesRestClientAddress 
                                  ?? BinanceApiAddresses.TestNet.RestClientAddress 
                },
                CoinFuturesApiOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.CoinFuturesRestClientAddress 
                                  ?? BinanceApiAddresses.TestNet.RestClientAddress
                }
            };
        }
        
        clientOptions.LogWriters.Add(logger);
        
        return clientOptions;
    }
    
    private static BinanceSocketClientOptions GetBinanceSocketClientOptions(AppSettings appSettings, 
        string apiKey, string secretKey, ILogger logger)
    {
        var clientOptions = new BinanceSocketClientOptions
        {
            ApiCredentials = new ApiCredentials(apiKey, secretKey),
            LogLevel = appSettings.Logger.SocketClientLogLevel
        };
        
        if (appSettings.Client.Server == ClientServer.TestNet)
        {
            clientOptions = new BinanceSocketClientOptions
            {
                SpotStreamsOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.SocketClientAddress
                },
                UsdFuturesStreamsOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.UsdFuturesSocketClientAddress 
                                  ?? BinanceApiAddresses.TestNet.SocketClientAddress
                },
                CoinFuturesStreamsOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.CoinFuturesSocketClientAddress 
                                  ?? BinanceApiAddresses.TestNet.SocketClientAddress
                }
            };
        }
        
        clientOptions.LogWriters.Add(logger);
        
        return clientOptions;
    }

    #endregion
}