using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Services;

namespace TradeHero.Client.CustomApi;

internal class ThCustomRestApi : IThCustomRestApi
{
    public IExchangeApi Exchange { get; }
    public IVolumeApi Volume { get; }
    public IKlineApi Kline { get; }
    public ISystemApi System { get; }

    public ThCustomRestApi(IThRestBinanceClient client, ICalculatorService calculatorService)
    {
        Exchange = new ExchangeApi(client, calculatorService);
        Volume = new VolumeApi(client, calculatorService);
        Kline = new KlineApi(client, calculatorService);
        System = new SystemApi(client);
    }
}