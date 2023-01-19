using Binance.Net.Clients;
using Binance.Net.Objects;
using Microsoft.Extensions.DependencyInjection;
using TradeHero.Client.CustomApi;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Client.CustomApi;
using TradeHero.Core.Types.Services;

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
}