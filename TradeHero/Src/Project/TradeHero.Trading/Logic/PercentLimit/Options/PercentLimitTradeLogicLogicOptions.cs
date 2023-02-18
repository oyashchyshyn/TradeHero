using Binance.Net.Enums;
using TradeHero.Core.Enums;
using TradeHero.Trading.Base;

namespace TradeHero.Trading.Logic.PercentLimit.Options;

internal class PercentLimitTradeLogicLogicOptions : BaseTradeLogicOptions
{
    public int Leverage { get; set; }
    public FuturesMarginType MarginType { get; set; }
    public int MaximumPositions { get; set; }
    public int MaximumPositionsPerIteration { get; set; }
    public decimal AvailableDepositPercentForTrading { get; set; }
    
    // Open Position
    public bool EnableOpenPositions { get; set; }
    public decimal PercentFromDepositForOpen { get; set; }
    public SignalSettings SignalSettingsForOpen { get; set; } = new();

    // Average Position
    public bool EnableAveraging { get; set; }
    public decimal AverageFromRoe { get; set; }
    public decimal AverageToRoe { get; set; }
    public SignalSettings SignalSettingsForAverage { get; set; } = new();

    // Trailing Stop
    public bool EnableTrailingStops { get; set; }
    public decimal CallbackRate { get; set; }
    public decimal TrailingStopRoe { get; set; }
    public decimal? MarketStopSafePriceFromLastPricePercent { get; set; }

    // Market Stop Close
    public bool EnableMarketStopToExit { get; set; }
    public decimal MarketStopExitRoeActivation { get; set; }
    public decimal MarketStopExitPriceFromLastPricePercent { get; set; }
    public decimal? MarketStopExitActivationFromAvailableBalancePercent { get; set; }
    public TimeSpan? MarketStopExitActivationAfterTime { get; set; }
    
    // Market Stop Close
    public bool EnableMarketStopLoss { get; set; }
    public decimal StopLossPercentFromDeposit { get; set; }
    public PositionSide StopLossForSide { get; set; }
}