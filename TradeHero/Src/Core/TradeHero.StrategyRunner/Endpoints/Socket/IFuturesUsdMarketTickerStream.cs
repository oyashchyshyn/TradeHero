using CryptoExchange.Net.Sockets;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.StrategyRunner;

namespace TradeHero.Strategies.Endpoints.Socket;

internal interface IFuturesUsdMarketTickerStream
{
    UpdateSubscription SocketSubscription { get; }

    Task<ActionResult> StartStreamMarketTickerAsync(ITradeLogicStore store, int maxRetries = 5,
        CancellationToken cancellationToken = default);
}