using Binance.Net.Interfaces.Clients;
using TradeHero.Contracts.Client.CustomApi;

namespace TradeHero.Contracts.Client;

public interface IThRestBinanceClient : IBinanceClient
{
    IThCustomRestApi CustomRestApi { get; }
}