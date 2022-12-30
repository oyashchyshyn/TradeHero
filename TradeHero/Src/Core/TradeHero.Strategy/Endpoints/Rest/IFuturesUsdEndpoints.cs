using Binance.Net.Enums;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Strategy;

namespace TradeHero.Strategies.Endpoints.Rest;

public interface IFuturesUsdEndpoints
{
    Task<ActionResult> SetFuturesUsdWalletBalancesAsync(IStrategyStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> SetFuturesUsdExchangeInfoAsync(IStrategyStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> SetFuturesUsdPositionInfoAsync(IStrategyStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> SetFuturesUsdStreamListenKeyAsync(IStrategyStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> ChangeMarginTypeToAllPositionsAsync(IStrategyStore store, FuturesMarginType marginType, CancellationToken cancellationToken = default);
    Task<ActionResult> ChangeLeverageToAllPositionsAsync(IStrategyStore store, int leverage, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> ChangeSymbolLeverageToAvailableAsync(string symbol, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> CancelOpenedOrdersAsync(string symbol, PositionSide side, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> UpdateStreamListerKeyAsync(IStrategyStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
    Task<ActionResult> DestroyStreamListerKeyAsync(IStrategyStore store, int maxRetries = 5, CancellationToken cancellationToken = default);
}