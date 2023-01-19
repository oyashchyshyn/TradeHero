using CryptoExchange.Net.Sockets;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Trading;

namespace TradeHero.Trading.Endpoints.Socket;

internal interface IFuturesUsdMarketTickerStream
{
    UpdateSubscription SocketSubscription { get; }

    Task<ActionResult> StartStreamMarketTickerAsync(ITradeLogicStore store, int maxRetries = 5,
        CancellationToken cancellationToken = default);
}