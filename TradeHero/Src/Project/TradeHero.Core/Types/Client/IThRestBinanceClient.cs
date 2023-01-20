using Binance.Net.Interfaces.Clients;
using TradeHero.Core.Types.Client.CustomApi;

namespace TradeHero.Core.Types.Client;

public interface IThRestBinanceClient : IBinanceClient
{
    IThCustomRestApi CustomRestApi { get; }
}