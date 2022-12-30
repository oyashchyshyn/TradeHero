using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Client.CustomApi;
using TradeHero.Client.CustomApi;
using TradeHero.Contracts.Services;

namespace TradeHero.Client.Clients;

internal class ThRestBinanceClient : BinanceClient, IThRestBinanceClient
{
    public IThCustomRestApi CustomRestApi { get; }
    
    public ThRestBinanceClient(
        BinanceClientOptions options, 
        IServiceProvider serviceProvider
        )
        : base(options)
    {
        CustomRestApi = new ThCustomRestApi(
            this, 
            serviceProvider.GetRequiredService<ICalculatorService>()
        );
    }

    public void SetCredentials(string key, string secret)
    {
        var options = new BinanceClientOptions
        {
            ApiCredentials = new ApiCredentials(key, secret)
        };

        SetDefaultOptions(options);
    }
}