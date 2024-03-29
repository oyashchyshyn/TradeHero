using System.ComponentModel;
using Binance.Net.Enums;
using Newtonsoft.Json;
using TradeHero.Application.Data.Dtos.Base;
using TradeHero.Core.Attributes;
using TradeHero.Core.Enums;

namespace TradeHero.Application.Data.Dtos.TradeLogic;

internal class PercentLimitTradeLogicDto : BaseStrategyDto
{
    [Description("Name for instance. Must be unique. Minimum length 3, Maximum lenght 40.")]
    [JsonProperty("name")]
    public override string Name { get; set; } = string.Empty;
    
    // General
    [Description("Leverage for position. Available range is 1 to 125.")]
    [JsonProperty("leverage")]
    public int Leverage { get; set; }
    
    [EnumDescription("Defines margin for position. Available values are '{0}'.", typeof(FuturesMarginType))]
    [JsonProperty("margin")]
    public FuturesMarginType MarginType { get; set; }
    
    [Description("Maximum position at ones. Available range is 0 to 1000.")]
    [JsonProperty("max_pos")]
    public int MaximumPositions { get; set; }
    
    [Description("Maximum allowed to open position per instance run. Available range is 0 to 1000.")]
    [JsonProperty("max_pos_per_instance")]
    public int MaximumPositionsPerIteration { get; set; }
    
    [Description("Percent of margin availalble for trading. Available range is 0.01 to 100.00.")]
    [JsonProperty("avl_balance_p")]
    public decimal AvailableDepositPercentForTrading { get; set; }

    // Open Position
    [Description("Enables automatic opening of positions. Bot will automatically open position.")]
    [JsonProperty("open_pos_enable")]
    public bool EnableOpenPositions { get; set; }
    
    [Description("Defining amount of margin for openning position. Available range is 0.1 to 100.0. Only works when 'open_pos_enable' enabled.")]
    [JsonProperty("open_pos_initial_margin_p")]
    public decimal PercentFromDepositForOpen { get; set; }
    
    [EnumDescription("Defines the action of signal for open position. Available values are '{0}'. Only works when 'open_pos_enable' enabled.", typeof(KlineSignalType))]
    [JsonProperty("open_pos_signal_type")]
    public KlineSignalType KlineSignalTypeForOpen { get; set; }

    [Description("Defining is POC must be only in wick of candles. Only works when 'open_pos_enable' enabled.")]
    [JsonProperty("open_poc_in_wick")]
    public bool IsPocMustBeInWickForOpen { get; set; }
    
    [Description("Average quote volume for candle to open position. Available range is 0.00 to 100000000.00. Only works when 'open_pos_enable' enabled.")]
    [JsonProperty("open_pos_min_quote_v")]
    public decimal MinQuoteVolumeForOpen { get; set; }

    [Description("Defining min value of trades in kline. Aavailalble range is 0 to 1000000. Only works when 'open_pos_enable' enabled.")]
    [JsonProperty("open_trades")]
    public decimal MinTradesForOpen { get; set; }
    
    [Description("Defining how greate the difference between sell and buy orders should be. If set to 0 bot will not check coefficient. Aavailalble range is 0.00 t0 100.00. Only works when 'open_pos_enable' enabled.")]
    [JsonProperty("open_coef_volume")]
    public decimal CoefficientOfVolumeForOpen { get; set; }
    
    [Description("Defining how greate the difference between sell and buy orders should be. If set to 0 bot will not check coefficient. Aavailalble range is 0.00 t0 100.00. Only works when 'open_pos_enable' enabled.")]
    [JsonProperty("open_coef_limits")]
    public decimal CoefficientOfOrderLimitsForOpen { get; set; }

    // Average Position
    [Description("Enables automatic average of position. Bot will automatically open aaverage orders by parameters for average.")]
    [JsonProperty("avg_enable")]
    public bool EnableAveraging { get; set; }
    
    [EnumDescription("Defines the action of signal for average position. Available values are '{0}'. Only works when 'avg_enable' enabled.", typeof(KlineSignalType))]
    [JsonProperty("avg_action_signal")]
    public KlineSignalType KlineSignalTypeForAverage { get; set; }

    [Description("From what percent of roe of position need to search for average. Available range is -10000.00 to 10000.00. Only works when 'avg_enable' enabled.")]
    [JsonProperty("avg_from_roe")]
    public decimal AverageFromRoe { get; set; }
    
    [Description("To what percent of roe of position need to make average. Available range is -10000.00 to 0.00. Only works when 'avg_enable' enabled.")]
    [JsonProperty("avg_to_roe")]
    public decimal AverageToRoe { get; set; }

    [Description("Defining is POC must be only in wich of candles. Only works when 'avg_enable' enabled.")]
    [JsonProperty("avg_poc_in_wick")]
    public bool IsPocMustBeInWickForAverage { get; set; }
    
    [Description("Average quote volume for candle to average position. Available range is 0.00 to 100000000.00. Only works when 'avg_enable' enabled.")]
    [JsonProperty("avg_min_quote_v")]
    public decimal MinQuoteVolumeForAverage { get; set; }

    [Description("Defining min value of trades in kline. Aavailalble range is 0 to 1000000. Only works when 'avg_enable' enabled.")]
    [JsonProperty("avg_trades")]
    public decimal MinTradesForAverage { get; set; }
    
    [Description("Defining how greate the difference between sell and buy orders should be. If set to 0 bot will not check coefficient. Aavailalble range is 0.00 t0 100.00. Only works when 'avg_enable' enabled.")]
    [JsonProperty("avg_coef_volume")]
    public decimal CoefficientOfVolumeForAverage { get; set; }
    
    [Description("Defining how greate the difference between bids and asks limits should be. If set to 0 bot will not check coefficient. Aavailalble range is 0.00 t0 100.00. Only works when 'avg_enable' enabled.")]
    [JsonProperty("avg_coef_limits")]
    public decimal CoefficientOfOrderLimitsForAverage { get; set; }
    
    // Trailing stop
    [Description("Enables trailing stop feature. Bot will automatically close position by parameter for trailing stop.")]
    [JsonProperty("ts_enable")]
    public bool EnableTrailingStops { get; set; }
    
    [Description("Roe of position when to place trailing stop order. Available range is -10000.00 to 10000.00. Only works when 'ts_enable' enabled.")]
    [JsonProperty("ts_from_roe")]
    public decimal TrailingStopRoe { get; set; }
    
    [Description("Callback rate for trailing stop. Available range is 0.1 to 5.0. Only works when 'ts_enable' enabled.")]
    [JsonProperty("ts_callback_rate")]
    public decimal CallbackRate { get; set; }

    [Description("Need for cases when ts is reached 'ts_from_roe' but you want to place an additional market stop order. Available range is 0.0 to 100.0. This parameter is optional, skip it if do not want to use it. Only works when 'ts_enable' enabled.")]
    [JsonProperty("ts_safe_market_stop")]
    public decimal? MarketStopSafePriceFromLastPricePercent { get; set; }

    // Stop limit
    [Description("Enables stop limit feature. Bot will automatically open stop limit order by parameter for stop limit.")]
    [JsonProperty("ms_enable")]
    public bool EnableMarketStopToExit { get; set; }
    
    [Description("Roe of position when to place stop limit order. Available range is -10000.00 to 10000.00. Only works when 'ms_enable' enabled.")]
    [JsonProperty("ms_from_roe")]
    public decimal MarketStopExitRoeActivation { get; set; }
    
    [Description("Defines how much percent need for price to go in order to fill stop limit order. Available range is 0.00 to 100.00. Only works when 'ms_enable' enabled.")]
    [JsonProperty("ms_from_price_p")]
    public decimal MarketStopExitPriceFromLastPricePercent { get; set; }
    
    [Description("Defines how much margin from blance percent need to have in margin to activate market stop feature. Available range is 0.00 to 100.00. This parameter is optional, skip it if do not want to use it. Only works when 'ms_enable' enabled.")]
    [JsonProperty("ms_from_balance_p")]
    public decimal? MarketStopExitActivationFromAvailableBalancePercent { get; set; }
    
    [Description("Defines how much time need to pass in order to activate market stop feature. Available range is 00:00:01 to 24:00:00. This parameter is optional, skip it if do not want to use it. Only works when 'ms_enable' enabled.")]
    [JsonProperty("ms_from_time")]
    public TimeSpan? MarketStopExitActivationAfterTime { get; set; }
    
    // Market Stop loss
    [Description("Enables stop loss feature. Bot will automatically close position when pnl reaches to percent from deposit.")]
    [JsonProperty("sl_enable")]
    public bool EnableMarketStopLoss { get; set; }
    
    [Description("Defines how much pecent from deposit can be lost for stop loss. Available range is 0.00 to 100.00. Only works when 'sl_enable' enabled.")]
    [JsonProperty("sl_from_balance_p")]
    public decimal StopLossPercentFromDeposit { get; set; }
    
    [EnumDescription("Defines side for stop loss feature. Available values are '{0}'. Only works when 'sl_enable' enabled.", typeof(PositionSide))]
    [JsonProperty("sl_side")]
    public PositionSide StopLossForSide { get; set; }
}