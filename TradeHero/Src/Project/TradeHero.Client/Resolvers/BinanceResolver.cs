using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using TradeHero.Client.Clients;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Client.Resolvers;

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
                ApiCredentials = new ApiCredentials(apiKey, secretKey)
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