using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Strategy.Models;
using TradeHero.Contracts.Strategy.Models.Instance;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Enums;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Models;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Options;

namespace TradeHero.Strategies.Strategies.PercentLimitsStrategy.Flow;

internal class PlsFilters
{
    private readonly ILogger<PlsFilters> _logger;
    private readonly ICalculatorService _calculatorService;
    private readonly IJsonService _jsonService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEnvironmentService _environmentService;

    public PlsFilters(
        ILogger<PlsFilters> logger,
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

    public async Task<IEnumerable<SymbolMarketInfo>> GetFilteredOrdersForOpenPositionAsync(InstanceResult instanceResult, PlsTradeLogicOptions tradeLogicOptions, 
        List<Position> openedPositions, List<BinancePositionDetailsUsdt> positionsInfo)
    {
        try
        {
            var topShortKlines = instanceResult.ShortSignals
                .Where(x => GetKlineActionsFromKlineActionSignal(tradeLogicOptions.KlineActionForOpen, x.KlinePositionSignal).Contains(x.KlineAction))
                .Where(x => GetKlinePowersFromKlinePowerSignal(tradeLogicOptions.KlinePowerForOpen, x.KlinePositionSignal).Contains(x.Power))
                .Where(x => x.TotalOrders >= tradeLogicOptions.MinTradesForOpen)
                .Where(x => x.KlineAverageTradeQuoteVolume >= tradeLogicOptions.MinQuoteVolumeForOpen)
                .Where(x => x.Asks.Sum(y => y.Quantity) > x.Bids.Sum(y => y.Quantity))
                .OrderByDescending(x => x.AsksBidsCoefficient)
                .ThenByDescending(x => x.PocOrdersCoefficient)
                .ToArray();
        
            var topLongKlines = instanceResult.LongSignals
                .Where(x => GetKlineActionsFromKlineActionSignal(tradeLogicOptions.KlineActionForOpen, x.KlinePositionSignal).Contains(x.KlineAction))
                .Where(x => GetKlinePowersFromKlinePowerSignal(tradeLogicOptions.KlinePowerForOpen, x.KlinePositionSignal).Contains(x.Power))
                .Where(x => x.TotalOrders >= tradeLogicOptions.MinTradesForOpen)
                .Where(x => x.KlineAverageTradeQuoteVolume >= tradeLogicOptions.MinQuoteVolumeForOpen)
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
                longsPositionsToOpen = tradeLogicOptions.MaximumPositionsPerIteration / 2;
                shortsPositionsToOpen = tradeLogicOptions.MaximumPositionsPerIteration / 2;
            }
            else if (instanceResult.ShortSignals.Count > instanceResult.LongSignals.Count)
            {
                var ration = (instanceResult.ShortSignals.Count == 0 ? 1 : instanceResult.ShortSignals.Count) 
                             / (instanceResult.LongSignals.Count == 0 ? 1 : instanceResult.LongSignals.Count);
                if (ration >= tradeLogicOptions.MaximumPositionsPerIteration)
                {
                    shortsPositionsToOpen = tradeLogicOptions.MaximumPositionsPerIteration;
                }
                else
                {
                    shortsPositionsToOpen = ration;
                    longsPositionsToOpen = tradeLogicOptions.MaximumPositionsPerIteration - ration;
                }
            }
            else
            {
                var ration = (instanceResult.LongSignals.Count == 0 ? 1 : instanceResult.LongSignals.Count) 
                             / (instanceResult.ShortSignals.Count == 0 ? 1 : instanceResult.ShortSignals.Count);
                if (ration >= tradeLogicOptions.MaximumPositionsPerIteration)
                {
                    longsPositionsToOpen = tradeLogicOptions.MaximumPositionsPerIteration;
                }
                else
                {
                    longsPositionsToOpen = ration;
                    shortsPositionsToOpen = tradeLogicOptions.MaximumPositionsPerIteration - ration;
                }
            }

            _logger.LogInformation("Maximum positions per iteration {IterCount}. Longs to check: {LongsCount}. Shorts to check: {ShortsCount}. In {Method}",
                tradeLogicOptions.MaximumPositionsPerIteration, longsPositionsToOpen, shortsPositionsToOpen, nameof(GetFilteredOrdersForOpenPositionAsync));
            
            var shortsToOpen = GetPositions(shortsPositionsToOpen, topShortKlines, openedPositions, positionsInfo, tradeLogicOptions);
            var longsToOpen = GetPositions(longsPositionsToOpen, topLongKlines, openedPositions, positionsInfo, tradeLogicOptions);
            
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
        BinanceFuturesUsdtSymbol symbolInfo, PlsTradeLogicOptions tradeLogicOptions)
    {
        try
        {
            if (!tradeLogicOptions.EnableAveraging)
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

            if (tradeLogicOptions.MinTradesForAverage > symbolMarketInfo.TotalOrders)
            {
                _logger.LogInformation("{Position}. Not valid total trades. Kline trades is {KlineTrades}. Accepted trades in options. {TradesInOptions}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.TotalOrders, tradeLogicOptions.MinTradesForAverage, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }
            
            if (tradeLogicOptions.MinQuoteVolumeForAverage > symbolMarketInfo.KlineAverageTradeQuoteVolume)
            {
                _logger.LogInformation("{Position}. Not valid trade quote volume. Kline trade asset volume is {TradeQuoteVolumeKline}. " +
                                       "Accepted trade quote volume in options. {TradeQuoteVolumeInOptions}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.KlineAverageTradeQuoteVolume, tradeLogicOptions.MinQuoteVolumeForAverage, 
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }
            
            if (!GetKlineActionsFromKlineActionSignal(tradeLogicOptions.KlineActionForAverage, openedPosition.PositionSide).Contains(symbolMarketInfo.KlineAction))
            {
                _logger.LogInformation("{Position}. Not valid kline action. Current kline action is {KlineAction}. " +
                                       "Kline action signal for average is {KlineActionSignal}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.KlineAction, tradeLogicOptions.KlineActionForAverage, 
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    
                return Task.FromResult(false);
            }
            
            if (!GetKlinePowersFromKlinePowerSignal(tradeLogicOptions.KlinePowerForAverage, openedPosition.PositionSide).Contains(symbolMarketInfo.Power))
            {
                _logger.LogInformation("{Position}. Not valid kline power. Current kline power is {KlinePower}. " +
                                       "Kline power signal for average is {KlinePowerSignal}. In {Method}", 
                    openedPosition.ToString(), symbolMarketInfo.Power, tradeLogicOptions.KlinePowerForAverage, 
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

            if (roePercent > tradeLogicOptions.AverageFromRoe)
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

    public OrderToPlace IsNeedToActivateOrders(Position openedPosition, decimal lastPrice, PositionInfo positionInfo, 
        BinanceFuturesAccountBalance balance, PlsTradeLogicOptions tradeLogicOptions)
    {
        try
        {
            var roePercent = _calculatorService.CalculateRoe(openedPosition.PositionSide, openedPosition.EntryPrice, 
                lastPrice, openedPosition.Leverage);

            // When trailing stop is activated
            if (positionInfo.IsTrailingStopActivated)
            {
                if (roePercent > positionInfo.HighestRoe)
                {
                    _logger.LogInformation("{Position}. Previous roe updated. Previous ROE: {Previous}%. New ROE: {New}%. In {Method}",
                        openedPosition.ToString(), positionInfo.HighestRoe, roePercent, nameof(IsNeedToActivateOrders));
                    
                    positionInfo.HighestRoe = roePercent;
                        
                    return OrderToPlace.None;
                }
                
                var callBackRateFromHighestRoe = positionInfo.HighestRoe - tradeLogicOptions.CallbackRate * openedPosition.Leverage;
                if (roePercent > callBackRateFromHighestRoe)
                {
                    return OrderToPlace.None;
                }
                
                _logger.LogInformation("{Position}. Market Close Position. ROE is valid and order will be placed. " +
                                       "Current ROE: {Roe}%. Highest ROE: {HighestRoe}%. In {Method}", 
                    openedPosition.ToString(), roePercent, positionInfo.HighestRoe, nameof(IsNeedToActivateOrders));
                    
                return OrderToPlace.MarketToClose;
            }

            // Market Stop logic
            if (tradeLogicOptions.EnableMarketStopToExit && roePercent >= tradeLogicOptions.MarketStopExitRoeActivation && positionInfo.IsNeedToPlaceMarketStop)
            {
                // By balance percent
                if (tradeLogicOptions.MarketStopExitActivationFromAvailableBalancePercent.HasValue 
                    && openedPosition.InitialMargin >= balance.AvailableBalance * tradeLogicOptions.MarketStopExitActivationFromAvailableBalancePercent / 100)
                {
                    _logger.LogInformation("{Position}. Market Stop to close by balance. ROE is valid and order will be placed. ROE: {Roe}%. In {Method}", 
                        openedPosition.ToString(), roePercent, nameof(IsNeedToActivateOrders));
                    
                    return OrderToPlace.MarketStopToClose;
                }
                
                // By time
                if (tradeLogicOptions.MarketStopExitActivationAfterTime.HasValue 
                    && openedPosition.LastUpdateTime.AddMilliseconds(tradeLogicOptions.MarketStopExitActivationAfterTime.Value.TotalMilliseconds) 
                    < _dateTimeService.GetUtcDateTime())
                {
                    var timeAfterAdding = openedPosition.LastUpdateTime.AddMilliseconds(
                        tradeLogicOptions.MarketStopExitActivationAfterTime.Value.TotalMilliseconds
                        );

                    _logger.LogInformation("{Position}. Market Stop to close by time. ROE is valid and order will be placed. " +
                                           "Position LasTime: {PositionTime}. Time after adding {AfterAdding}. Utc time {UtcTime}. ROE: {Roe}%. In {Method}", 
                        openedPosition.ToString(), openedPosition.LastUpdateTime, timeAfterAdding, _dateTimeService.GetUtcDateTime(), 
                        roePercent, nameof(IsNeedToActivateOrders));
                    
                    return OrderToPlace.MarketStopToClose;
                }
            }

            // When Trailing stop is not activated
            if (tradeLogicOptions.EnableTrailingStops && roePercent >= tradeLogicOptions.TrailingStopRoe && !positionInfo.IsTrailingStopActivated)
            {
                positionInfo.IsTrailingStopActivated = true;
                
                positionInfo.HighestRoe = roePercent;
                
                _logger.LogInformation("{Position}. Trailing stop check activated. ROE: {Roe}%. In {Method}",
                    openedPosition.ToString(), roePercent, nameof(IsNeedToActivateOrders));

                if (!positionInfo.IsNeedToPlaceMarketStop || !tradeLogicOptions.MarketStopSafePriceFromLastPricePercent.HasValue)
                {
                    return OrderToPlace.None;
                }

                _logger.LogInformation("{Position}. Safe stop loss will be placed. In {Method}",
                    openedPosition.ToString(), nameof(IsNeedToActivateOrders));

                return OrderToPlace.MarketStopToSafe;
            }

            return OrderToPlace.None;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(IsNeedToActivateOrders));

            return OrderToPlace.None;
        }
    }

    #region Private methods

    private List<SymbolMarketInfo> GetPositions(int toOpen, IEnumerable<SymbolMarketInfo> klineInfos, IReadOnlyCollection<Position> openedPositions, 
        IReadOnlyCollection<BinancePositionDetailsUsdt> positionsInfo, PlsTradeLogicOptions tradeLogicOptions)
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
                && (x.Leverage != tradeLogicOptions.Leverage || x.MarginType != tradeLogicOptions.MarginType)))
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