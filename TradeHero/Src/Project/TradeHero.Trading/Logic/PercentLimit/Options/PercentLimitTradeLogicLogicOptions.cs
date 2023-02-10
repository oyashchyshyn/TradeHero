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
    public KlineActionSignal KlineActionForOpen { get; set; }
    public KlinePowerSignal KlinePowerForOpen { get; set; }
    public decimal PercentFromDepositForOpen { get; set; }
    public decimal MinQuoteVolumeForOpen { get; set; }
    public bool IsPocMustBeInWickForOpen { get; set; }

    // Average Position
    public bool EnableAveraging { get; set; }
    public KlineActionSignal KlineActionForAverage { get; set; }
    public KlinePowerSignal KlinePowerForAverage { get; set; }
    public decimal AverageFromRoe { get; set; }
    public decimal AverageToRoe { get; set; }
    public decimal MinQuoteVolumeForAverage { get; set; }
    public bool IsPocMustBeInWickForAverage { get; set; }

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