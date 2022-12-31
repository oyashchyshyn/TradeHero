using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Settings;
using TradeHero.Client.Clients;
using TradeHero.Client.Resolvers;
using TradeHero.Contracts.Client.Resolvers;

namespace TradeHero.Client;

public static class ThClientServiceCollectionExtensions
{
    public static void AddThClient(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IThRestBinanceClient, ThRestBinanceClient>(serviceProvider =>
        {
            var connectionRepository = serviceProvider.GetRequiredService<IConnectionRepository>();
            var connection = connectionRepository.GetActiveConnection();
            
            var appSettings = serviceProvider.GetRequiredService<AppSettings>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var restClientOptions = GetBinanceClientOptions(appSettings, connection, loggerFactory.CreateLogger<ThRestBinanceClient>());
            
            return new ThRestBinanceClient(restClientOptions, serviceProvider);
        });
        
        serviceCollection.AddTransient<IThSocketBinanceClient, ThSocketBinanceClient>(serviceProvider =>
        {
            var connectionRepository = serviceProvider.GetRequiredService<IConnectionRepository>();
            var connection = connectionRepository.GetActiveConnection();
            
            var appSettings = serviceProvider.GetRequiredService<AppSettings>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var socketClientOptions = GetBinanceSocketClientOptions(appSettings, connection, loggerFactory.CreateLogger<ThSocketBinanceClient>());

            return new ThSocketBinanceClient(socketClientOptions);
        });

        serviceCollection.AddSingleton<IBinanceResolver, BinanceResolver>();
    }

    #region Private methods

    private static BinanceClientOptions GetBinanceClientOptions(AppSettings appSettings, ConnectionDto connection, ILogger logger)
    {
        var clientOptions = new BinanceClientOptions
        {
            ApiCredentials = new ApiCredentials(connection.ApiKey, connection.SecretKey),
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
                    BaseAddress = BinanceApiAddresses.TestNet.UsdFuturesRestClientAddress ?? BinanceApiAddresses.TestNet.RestClientAddress 
                },
                CoinFuturesApiOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.CoinFuturesRestClientAddress ?? BinanceApiAddresses.TestNet.RestClientAddress
                }
            };
        }
        
        clientOptions.LogWriters.Add(logger);
        
        return clientOptions;
    }
    
    private static BinanceSocketClientOptions GetBinanceSocketClientOptions(AppSettings appSettings, ConnectionDto connection, ILogger logger)
    {
        var clientOptions = new BinanceSocketClientOptions
        {
            ApiCredentials = new ApiCredentials(connection.ApiKey, connection.SecretKey),
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
                    BaseAddress = BinanceApiAddresses.TestNet.UsdFuturesSocketClientAddress ?? BinanceApiAddresses.TestNet.SocketClientAddress
                },
                CoinFuturesStreamsOptions =
                {
                    BaseAddress = BinanceApiAddresses.TestNet.CoinFuturesSocketClientAddress ?? BinanceApiAddresses.TestNet.SocketClientAddress
                }
            };
        }
        
        clientOptions.LogWriters.Add(logger);
        
        return clientOptions;
    }

    #endregion
}