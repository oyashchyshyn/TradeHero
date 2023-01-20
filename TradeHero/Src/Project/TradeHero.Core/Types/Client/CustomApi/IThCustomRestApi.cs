namespace TradeHero.Core.Types.Client.CustomApi;

public interface IThCustomRestApi
{
    IExchangeApi Exchange { get; }
    IVolumeApi Volume { get; }
    IKlineApi Kline { get; }
    ISystemApi System { get; }
}