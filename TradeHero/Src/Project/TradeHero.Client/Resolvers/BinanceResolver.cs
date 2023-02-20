using Binance.Net.Objects;
using Microsoft.Extensions.Logging;
using TradeHero.Client.Clients;
using TradeHero.Core.Contracts.Client;

namespace TradeHero.Client.Resolvers;

internal class BinanceResolver : IBinanceResolver
{
    private readonly ILogger<BinanceResolver> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BinanceResolver(
        ILogger<BinanceResolver> logger, 
        IServiceProvider serviceProvider
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public IThRestBinanceClient? GenerateBinanceClient(string apiKey, string secretKey)
    {
        try
        {
            var options = new BinanceClientOptions
            {
                ApiCredentials = new BinanceApiCredentials(apiKey, secretKey)
            };

            return new ThRestBinanceClient(options, _serviceProvider);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GenerateBinanceClient));

            return null;
        }
    }
}