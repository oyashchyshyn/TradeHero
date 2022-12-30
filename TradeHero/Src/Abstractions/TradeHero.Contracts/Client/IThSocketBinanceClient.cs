using Binance.Net.Interfaces.Clients;

namespace TradeHero.Contracts.Client;

public interface IThSocketBinanceClient : IBinanceSocketClient
{
    void SetCredentials(string key, string secret);
}