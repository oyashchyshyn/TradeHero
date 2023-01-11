using Binance.Net.Objects.Models.Futures.Socket;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Trading.Models.Args;

public class FuturesUsdOrderReceiveArgs
{
    public BinanceFuturesStreamOrderUpdateData OrderUpdate { get; }
    public OrderReceiveType OrderReceiveType { get; }

    public FuturesUsdOrderReceiveArgs(BinanceFuturesStreamOrderUpdateData orderUpdate, OrderReceiveType orderReceiveType)
    {
        OrderUpdate = orderUpdate;
        OrderReceiveType = orderReceiveType;
    }
}