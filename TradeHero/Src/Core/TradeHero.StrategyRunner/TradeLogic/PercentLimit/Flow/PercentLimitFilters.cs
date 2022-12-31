using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.StrategyRunner.Models;
using TradeHero.Contracts.StrategyRunner.Models.Instance;
using TradeHero.Strategies.TradeLogic.PercentLimit.Enums;
using TradeHero.Strategies.TradeLogic.PercentLimit.Models;
using TradeHero.Strategies.TradeLogic.PercentLimit.Options;

namespace TradeHero.Strategies.TradeLogic.PercentLimit.Flow;

internal class PercentLimitFilters
{
    private readonly ILogger<PercentLimitFilters> _logger;
    private readonly ICalculatorService _calculatorService;
    private readonly IJsonService _jsonService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEnvironmentService _environmentService;

    public PercentLimitFilters(
        ILogger<PercentLimitFilters> logger,
        ICalculatorService calculatorService, 
        IJsonService jsonService,  
        IDateTimeService dateTimeService,
        IEnvironmentService environmentService
        )
    {
        _logger = logger;
        _calculatorService = calculatorService;
        _jsonService = jsonService;
        _dateTimeService = dateTimeService;
        _environmentService = environmentService;
    }

    public async Task<IEnumerable<SymbolMarketInfo>> GetFilteredOrdersForOpenPositionAsync(InstanceResult instanceResult, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions, 
        List<Position> openedPositions, List<BinancePositionDetailsUsdt> positionsInfo)
    {
        try
        {
            var topShortKlines = instanceResult.ShortSignals
                .Where(x => GetKlineActionsFromKlineActionSignal(tradeLogicLogicOptions.KlineActionForOpen, x.KlinePositionSignal).Contains(x.KlineAction))
                .Where(x => GetKlinePowersFromKlinePowerSignal(tradeLogicLogicOptions.KlinePowerForOpen, x.KlinePositionSignal).Contains(x.Power))
                .Where(x => x.TotalOrders >= tradeLogicLogicOptions.MinTradesForOpen)
                .Where(x => x.KlineAverageTradeQuoteVolume >= tradeLogicLogicOptions.MinQuoteVolumeForOpen)
                .Where(x => x.Asks.Sum(y => y.Quantity) > x.Bids.Sum(y => y.Quantity))
                .OrderByDescending(x => x.AsksBidsCoefficient)
                .ThenByDescending(x => x.PocOrdersCoefficient)
                .ToArray();
        
            var topLongKlines = instanceResult.LongSignals
                .Where(x => GetKlineActionsFromKlineActionSignal(tradeLogicLogicOptions.KlineActionForOpen, x.KlinePositionSignal).Contains(x.KlineAction))
                .Where(x => GetKlinePowersFromKlinePowerSignal(tradeLogicLogicOptions.KlinePowerForOpen, x.KlinePositionSignal).Contains(x.Power))
                .Where(x => x.TotalOrders >= tradeLogicLogicOptions.MinTradesForOpen)
                .Where(x => x.KlineAverageTradeQuoteVolume >= tradeLogicLogicOptions.MinQuoteVolumeForOpen)
                .Where(x => x.Bids.Sum(y => y.Quantity) > x.Asks.Sum(y => y.Quantity))
                .OrderByDescending(x => x.AsksBidsCoefficient)
                .ThenByDescending(x => x.PocOrdersCoefficient)
                .ToArray();

            _logger.LogInformation("Filtered Longs: {FilteredLongsCount}. Filtered Shorts: {FilteredShortsCount}. In {Method}",
                topLongKlines.Length, topShortKlines.Length, nameof(GetFilteredOrdersForOpenPositionAsync));
            
            var folderName = Path.Combine(_environmentService.GetBasePath(), FolderConstants.DataFolder, "ClusterResults");
            var jsonShorts = _jsonService.SerializeObject(instanceResult.ShortSignals, Formatting.Indented).Data;
            var jsonLongs = _jsonService.SerializeObject(instanceResult.LongSignals, Formatting.Indented).Data;
            var jsonFilteredShorts = _jsonService.SerializeObject(topShortKlines, Formatting.Indented).Data;
            var jsonFilteredLongs = _jsonService.SerializeObject(topLongKlines, Formatting.Indented).Data;

            var messagePositions = $"SHORTS:{Environment.NewLine}{jsonShorts}{Environment.NewLine}LONGS:{Environment.NewLine}{jsonLongs}";
            var messagePositionsToOpen = $"SHORTS:{Environment.NewLine}{jsonFilteredShorts}{Environment.NewLine}LONGS:{Environment.NewLine}{jsonFilteredLongs}";

            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            
            await File.WriteAllTextAsync(
                Path.Combine(folderName, $"{_dateTimeService.GetUtcDateTime():dd_MM_yyyy_HH_mm_ss}.json"), 
                string.Join($"{Environment.NewLine}------------------------{Environment.NewLine}", 
                    messagePositions, messagePositionsToOpen)
            );
            
            var shortsPositionsToOpen = 0;
            var longsPositionsToOpen = 0;

            if (instanceResult.ShortSignals.Count == instanceResult.LongSignals.Count)
            {
                longsPositionsToOpen = tradeLogicLogicOptions.MaximumPositionsPerIteration / 2;
                shortsPositionsToOpen = tradeLogicLogicOptions.MaximumPositionsPerIteration / 2;
            }
            else if (instanceResult.ShortSignals.Count > instanceResult.LongSignals.Count)
            {
                var ration = (instanceResult.ShortSignals.Count == 0 ? 1 : instanceResult.ShortSignals.Count) 
                             / (instanceResult.LongSignals.Count == 0 ? 1 : instanceResult.LongSignals.Count);
                if (ration >= tradeLogicLogicOptions.MaximumPositionsPerIteration)
                {
                    shortsPositionsToOpen = tradeLogicLogicOptions.MaximumPositionsPerIteration;
                }
                else
                {
                    shortsPositionsToOpen = ration;
                    longsPositionsToOpen = tradeLogicLogicOptions.MaximumPositionsPerIteration - ration;
                }
            }
            else
            {
                var ration = (instanceResult.LongSignals.Count == 0 ? 1 : instanceResult.LongSignals.Count) 
                             / (instanceResult.ShortSignals.Count == 0 ? 1 : instanceResult.ShortSignals.Count);
                if (ration >= tradeLogicLogicOptions.MaximumPositionsPerIteration)
                {
                    longsPositionsToOpen = tradeLogicLogicOptions.MaximumPositionsPerIteration;
                }
                else
                {
                    longsPositionsToOpen = ration;
                    shortsPositionsToOpen = tradeLogicLogicOptions.MaximumPositionsPerIteration - ration;
                }
            }

            _logger.LogInformation("Maximum positions per iteration {IterCount}. Longs to check: {LongsCount}. Shorts to check: {ShortsCount}. In {Method}",
                tradeLogicLogicOptions.MaximumPositionsPerIteration, longsPositionsToOpen, shortsPositionsToOpen, nameof(GetFilteredOrdersForOpenPositionAsync));
            
            var shortsToOpen = GetPositions(shortsPositionsToOpen, topShortKlines, openedPositions, positionsInfo, tradeLogicLogicOptions);
            var longsToOpen = GetPositions(longsPositionsToOpen, topLongKlines, openedPositions, positionsInfo, tradeLogicLogicOptions);
            
            _logger.LogInformation("Longs to open: {LongsCount}. Shorts to open: {ShortsCount}. In {Method}",
                longsToOpen.Count, shortsToOpen.Count, nameof(GetFilteredOrdersForOpenPositionAsync));

            return shortsToOpen.Concat(longsToOpen);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetFilteredOrdersForOpenPositionAsync));

            return new List<SymbolMarketInfo>();
        }
    }

    public Task<bool> IsNeedToPlaceMarketAverageOrderAsync(InstanceResult instanceResult, Position openedPosition, decimal lastPrice, SymbolMarketInfo symbolMarketInfo, 
        BinanceFuturesUsdtSymbol symbolInfo, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
    {
        try
        {
            if (!tradeLogicLogicOptions.EnableAveraging)
            {
                _logger.LogError("{Position}. Averaging is disabled. In {Method}",
                    openedPosition.ToString(), nameof(IsNeedToPlaceMarketAverageOrderAsync));
                
                return Task.FromResult(false);
            }
            
            if (symbolInfo.PriceFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    openedPosition.ToString(), nameof(symbolInfo.PriceFilter), nameof(IsNeedToPlaceMarketAverageOrderAsync));
                
                return Task.FromResult(false);
            }
            
            if (openedPosition.PositionSide != symbolMarketInfo.KlinePositionSignal)
            {
                _logger.LogInformation("{Position}. Not valid side for average. Kline side is {KlineSide}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.KlinePositionSignal, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }

            if (tradeLogicLogicOptions.MinTradesForAverage > symbolMarketInfo.TotalOrders)
            {
                _logger.LogInformation("{Position}. Not valid total trades. Kline trades is {KlineTrades}. Accepted trades in options. {TradesInOptions}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.TotalOrders, tradeLogicLogicOptions.MinTradesForAverage, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }
            
            if (tradeLogicLogicOptions.MinQuoteVolumeForAverage > symbolMarketInfo.KlineAverageTradeQuoteVolume)
            {
                _logger.LogInformation("{Position}. Not valid trade quote volume. Kline trade asset volume is {TradeQuoteVolumeKline}. " +
                                       "Accepted trade quote volume in options. {TradeQuoteVolumeInOptions}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.KlineAverageTradeQuoteVolume, tradeLogicLogicOptions.MinQuoteVolumeForAverage, 
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }
            
            if (!GetKlineActionsFromKlineActionSignal(tradeLogicLogicOptions.KlineActionForAverage, openedPosition.PositionSide).Contains(symbolMarketInfo.KlineAction))
            {
                _logger.LogInformation("{Position}. Not valid kline action. Current kline action is {KlineAction}. " +
                                       "Kline action signal for average is {KlineActionSignal}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.KlineAction, tradeLogicLogicOptions.KlineActionForAverage, 
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }
            
            if (!GetKlinePowersFromKlinePowerSignal(tradeLogicLogicOptions.KlinePowerForAverage, openedPosition.PositionSide).Contains(symbolMarketInfo.Power))
            {
                _logger.LogInformation("{Position}. Not valid kline power. Current kline power is {KlinePower}. " +
                                       "Kline power signal for average is {KlinePowerSignal}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.Power, tradeLogicLogicOptions.KlinePowerForAverage, 
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }
            
            var asksTotalQuantity = symbolMarketInfo.Asks.Sum(x => x.Quantity);
            var bidsTotalQuantity = symbolMarketInfo.Bids.Sum(x => x.Quantity);

            if (symbolMarketInfo.KlinePositionSignal == PositionSide.Short && asksTotalQuantity <= bidsTotalQuantity
                || symbolMarketInfo.KlinePositionSignal == PositionSide.Long && bidsTotalQuantity <= asksTotalQuantity)
            {
                _logger.LogInformation("{Position}. Not valid Bids and Asks coefficient. Kline side is {KlineSide}. Asks: {Asks}. Bids {Bids}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.KlinePositionSignal, asksTotalQuantity, 
                    bidsTotalQuantity, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }

            var roePercent = _calculatorService.CalculateRoe(openedPosition.PositionSide, openedPosition.EntryPrice, 
                lastPrice, openedPosition.Leverage);

            if (roePercent > tradeLogicLogicOptions.AverageFromRoe)
            {
                _logger.LogInformation("{Position}. Roe percent is invalid. ROE: {Roe}%. In {Method}",
                    openedPosition.ToString(), roePercent, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                
                return Task.FromResult(false);
            }

            switch (instanceResult.MarketMood)
            {
                case MarketMood.Short when openedPosition.PositionSide == PositionSide.Long:
                case MarketMood.Long when openedPosition.PositionSide == PositionSide.Short:
                case MarketMood.Balanced:
                    _logger.LogInformation("{Position}. Wrong market mood. Current market mood is {MarketMood}. In {Method}", 
                        openedPosition.ToString(), instanceResult.MarketMood, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    return Task.FromResult(false);
            }
            
            _logger.LogInformation("{Position}. ROE is valid and order will be placed. ROE: {Roe}%. In {Method}", 
                openedPosition.ToString(), roePercent, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                
            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(IsNeedToPlaceMarketAverageOrderAsync));

            return Task.FromResult(false);
        }
    }

    public PercentLimitOrderToPlace IsNeedToActivateOrders(Position openedPosition, decimal lastPrice, PercentLimitPositionInfo percentLimitPositionInfo, 
        BinanceFuturesAccountBalance balance, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
    {
        try
        {
            var roePercent = _calculatorService.CalculateRoe(openedPosition.PositionSide, openedPosition.EntryPrice, 
                lastPrice, openedPosition.Leverage);

            // When trailing stop is activated
            if (percentLimitPositionInfo.IsTrailingStopActivated)
            {
                if (roePercent > percentLimitPositionInfo.HighestRoe)
                {
                    _logger.LogInformation("{Position}. Previous roe updated. Previous ROE: {Previous}%. New ROE: {New}%. In {Method}",
                        openedPosition.ToString(), percentLimitPositionInfo.HighestRoe, roePercent, nameof(IsNeedToActivateOrders));
                    
                    percentLimitPositionInfo.HighestRoe = roePercent;
                        
                    return PercentLimitOrderToPlace.None;
                }
                
                var callBackRateFromHighestRoe = percentLimitPositionInfo.HighestRoe - tradeLogicLogicOptions.CallbackRate * openedPosition.Leverage;
                if (roePercent > callBackRateFromHighestRoe)
                {
                    return PercentLimitOrderToPlace.None;
                }
                
                _logger.LogInformation("{Position}. Market Close Position. ROE is valid and order will be placed. " +
                                       "Current ROE: {Roe}%. Highest ROE: {HighestRoe}%. In {Method}", 
                    openedPosition.ToString(), roePercent, percentLimitPositionInfo.HighestRoe, nameof(IsNeedToActivateOrders));
                    
                return PercentLimitOrderToPlace.MarketToClose;
            }

            // Market Stop logic
            if (tradeLogicLogicOptions.EnableMarketStopToExit && roePercent >= tradeLogicLogicOptions.MarketStopExitRoeActivation && percentLimitPositionInfo.IsNeedToPlaceMarketStop)
            {
                // By balance percent
                if (tradeLogicLogicOptions.MarketStopExitActivationFromAvailableBalancePercent.HasValue 
                    && openedPosition.InitialMargin >= balance.AvailableBalance * tradeLogicLogicOptions.MarketStopExitActivationFromAvailableBalancePercent / 100)
                {
                    _logger.LogInformation("{Position}. Market Stop to close by balance. ROE is valid and order will be placed. ROE: {Roe}%. In {Method}", 
                        openedPosition.ToString(), roePercent, nameof(IsNeedToActivateOrders));
                    
                    return PercentLimitOrderToPlace.MarketStopToClose;
                }
                
                // By time
                if (tradeLogicLogicOptions.MarketStopExitActivationAfterTime.HasValue 
                    && openedPosition.LastUpdateTime.AddMilliseconds(tradeLogicLogicOptions.MarketStopExitActivationAfterTime.Value.TotalMilliseconds) 
                    < _dateTimeService.GetUtcDateTime())
                {
                    var timeAfterAdding = openedPosition.LastUpdateTime.AddMilliseconds(
                        tradeLogicLogicOptions.MarketStopExitActivationAfterTime.Value.TotalMilliseconds
                        );

                    _logger.LogInformation("{Position}. Market Stop to close by time. ROE is valid and order will be placed. " +
                                           "Position LasTime: {PositionTime}. Time after adding {AfterAdding}. Utc time {UtcTime}. ROE: {Roe}%. In {Method}", 
                        openedPosition.ToString(), openedPosition.LastUpdateTime, timeAfterAdding, _dateTimeService.GetUtcDateTime(), 
                        roePercent, nameof(IsNeedToActivateOrders));
                    
                    return PercentLimitOrderToPlace.MarketStopToClose;
                }
            }

            // When Trailing stop is not activated
            if (tradeLogicLogicOptions.EnableTrailingStops && roePercent >= tradeLogicLogicOptions.TrailingStopRoe && !percentLimitPositionInfo.IsTrailingStopActivated)
            {
                percentLimitPositionInfo.IsTrailingStopActivated = true;
                
                percentLimitPositionInfo.HighestRoe = roePercent;
                
                _logger.LogInformation("{Position}. Trailing stop check activated. ROE: {Roe}%. In {Method}",
                    openedPosition.ToString(), roePercent, nameof(IsNeedToActivateOrders));

                if (!percentLimitPositionInfo.IsNeedToPlaceMarketStop || !tradeLogicLogicOptions.MarketStopSafePriceFromLastPricePercent.HasValue)
                {
                    return PercentLimitOrderToPlace.None;
                }

                _logger.LogInformation("{Position}. Safe stop loss will be placed. In {Method}",
                    openedPosition.ToString(), nameof(IsNeedToActivateOrders));

                return PercentLimitOrderToPlace.MarketStopToSafe;
            }

            return PercentLimitOrderToPlace.None;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(IsNeedToActivateOrders));

            return PercentLimitOrderToPlace.None;
        }
    }

    #region Private methods

    private List<SymbolMarketInfo> GetPositions(int toOpen, IEnumerable<SymbolMarketInfo> klineInfos, IReadOnlyCollection<Position> openedPositions, 
        IReadOnlyCollection<BinancePositionDetailsUsdt> positionsInfo, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
    {
        if (toOpen == 0)
        {
            return new List<SymbolMarketInfo>();
        }
        
        var counter = 0;
        var list = new List<SymbolMarketInfo>();
        foreach (var position in klineInfos)
        {
            if (counter >= toOpen)
            {
                break;
            }

            if (openedPositions.Any(x => x.Name == position.FuturesUsdName && x.PositionSide == position.KlinePositionSignal))
            {
                continue;
            }

            if (positionsInfo.Any(x => x.Symbol == position.FuturesUsdName 
                && (x.Leverage != tradeLogicLogicOptions.Leverage || x.MarginType != tradeLogicLogicOptions.MarginType)))
            {
                _logger.LogWarning("Skip to open position because leverage or margin types are different. In {Method}",
                    nameof(GetPositions));
                
                continue;
            }
                
            list.Add(position);

            counter++;
        }

        return list;
    }

    private static List<KlineAction> GetKlineActionsFromKlineActionSignal(KlineActionSignal klineActionSignal, PositionSide positionSide)
    {
        return klineActionSignal switch
        {
            KlineActionSignal.Low when positionSide == PositionSide.Short => new List<KlineAction>
            {
                KlineAction.StopSlow, KlineAction.Stop, KlineAction.StopStrong
            },
            KlineActionSignal.Low when positionSide == PositionSide.Long => new List<KlineAction>
            {
                KlineAction.PushSlow, KlineAction.Push, KlineAction.PushStrong
            },
            KlineActionSignal.Middle when positionSide == PositionSide.Short => new List<KlineAction>
            {
                KlineAction.Stop, KlineAction.StopStrong
            },
            KlineActionSignal.Middle when positionSide == PositionSide.Long => new List<KlineAction>
            {
                KlineAction.Push, KlineAction.PushStrong
            },
            KlineActionSignal.Strong when positionSide == PositionSide.Short => new List<KlineAction>
            {
                KlineAction.StopStrong
            },
            KlineActionSignal.Strong when positionSide == PositionSide.Long => new List<KlineAction>
            {
                KlineAction.PushStrong
            },
            _ => new List<KlineAction>
            {
                KlineAction.None
            }
        };
    }
    
    private static List<KlinePower> GetKlinePowersFromKlinePowerSignal(KlinePowerSignal klinePowerSignal, PositionSide positionSide)
    {
        return klinePowerSignal switch
        {
            KlinePowerSignal.AccordingToPosition when positionSide == PositionSide.Short => new List<KlinePower>
            {
                KlinePower.Bear
            },
            KlinePowerSignal.AccordingToPosition when positionSide == PositionSide.Long => new List<KlinePower>
            {
                KlinePower.Bull
            },
            KlinePowerSignal.ReversalToPosition when positionSide == PositionSide.Short => new List<KlinePower>
            {
                KlinePower.Bull
            },
            KlinePowerSignal.ReversalToPosition when positionSide == PositionSide.Long => new List<KlinePower>
            {
                KlinePower.Bear
            },
            KlinePowerSignal.Any => new List<KlinePower>
            {
                KlinePower.Bear, KlinePower.Bull
            },
            _ => new List<KlinePower>
            {
                KlinePower.Bear, KlinePower.Bull
            }
        };
    }

    #endregion
}