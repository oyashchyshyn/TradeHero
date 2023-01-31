using Binance.Net.Interfaces.Clients;

namespace TradeHero.Core.Contracts.Client;

public interface IThRestBinanceClient : IBinanceClient
{
    IThCustomRestApi CustomRestApi { get; }
}