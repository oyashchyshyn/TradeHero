using Binance.Net.Enums;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.StrategyRunner;

namespace TradeHero.StrategyRunner.Endpoints.Rest;

public interface IFuturesUsdEndpoints
{
    Task<ActionResult> SetFuturesUsdWalletBalancesAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> SetFuturesUsdExchangeInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> SetFuturesUsdPositionInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> SetFuturesUsdStreamListenKeyAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> ChangeMarginTypeToAllPositionsAsync(ITradeLogicStore store, FuturesMarginType marginType, CancellationToken cancellationToken = default);
    Task<ActionResult> ChangeLeverageToAllPositionsAsync(ITradeLogicStore store, int leverage, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> ChangeSymbolLeverageToAvailableAsync(string symbol, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> CancelOpenedOrdersAsync(string symbol, PositionSide side, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> UpdateStreamListerKeyAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> DestroyStreamListerKeyAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
}