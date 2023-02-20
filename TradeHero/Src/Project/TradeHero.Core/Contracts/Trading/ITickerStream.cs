using CryptoExchange.Net.Sockets;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Contracts.Trading;

public interface ITickerStream
{
    UpdateSubscription SocketSubscription { get; }
    public bool IsConnected { get; }
    Task<ActionResult> StartStreamSymbolTickerAsync(string symbol, int maxRetries = 5, CancellationToken cancellationToken = default);
}