namespace TradeHero.Core.Contracts.Client;

public interface IThCustomRestApi
{
    IExchangeApi Exchange { get; }
    IVolumeApi Volume { get; }
    IKlineApi Kline { get; }
    ISystemApi System { get; }
}