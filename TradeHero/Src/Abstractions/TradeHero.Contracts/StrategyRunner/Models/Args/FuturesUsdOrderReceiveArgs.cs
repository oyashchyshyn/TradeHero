using Binance.Net.Objects.Models.Futures.Socket;
using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Contracts.StrategyRunner.Models.Args;

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