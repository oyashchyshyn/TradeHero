using CryptoExchange.Net.Sockets;
using TradeHero.Contracts.StrategyRunner;
using TradeHero.Core.Enums;

namespace TradeHero.StrategyRunner.Endpoints.Socket;

internal interface IFuturesUsdMarketTickerStream
{
    UpdateSubscription SocketSubscription { get; }

    Task<ActionResult> StartStreamMarketTickerAsync(ITradeLogicStore store, int maxRetries = 5,
        CancellationToken cancellationToken = default);
}