using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradeHero.Core.Constants;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Models.Trading;
using TradeHero.Trading.Logic.PercentLimit.Enums;
using TradeHero.Trading.Logic.PercentLimit.Models;
using TradeHero.Trading.Logic.PercentLimit.Options;

namespace TradeHero.Trading.Logic.PercentLimit.Flow;

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

    public async Task<List<SignalInfo>> GetFilteredOrdersForOpenPositionAsync(InstanceResult instanceResult,
        PercentLimitTradeLogicLogicOptions options, IReadOnlyCollection<Position> openedPositions,
        IReadOnlyCollection<BinancePositionDetailsUsdt> positionsInfo)
    {
        try
        {
            var shortSignals = GetShortSignalsForOpen(instanceResult, options);
            var longSignals = GetLongSignalsForOpen(instanceResult, options);

            _logger.LogInformation("Filtered Longs: {FilteredLongsCount}. Filtered Shorts: {FilteredShortsCount}. In {Method}",
                shortSignals.Count, longSignals.Count, nameof(GetFilteredOrdersForOpenPositionAsync));

            var folderName = Path.Combine(_environmentService.GetBasePath(), FolderConstants.ClusterResultsFolder);
            
            var jsonSignals = _jsonService.SerializeObject(instanceResult.Signals, Formatting.Indented).Data;
            var jsonShorts = _jsonService.SerializeObject(shortSignals, Formatting.Indented).Data;
            var jsonLongs = _jsonService.SerializeObject(longSignals, Formatting.Indented).Data;

            var newLine = Environment.NewLine;
            
            var messagePositions = $"SIGNALS:{newLine}{jsonSignals}{newLine}FILTERED SHORTS:{newLine}{jsonShorts}{newLine}FILTERED LONGS:{newLine}{jsonLongs}";

            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            await File.WriteAllTextAsync(Path.Combine(folderName, $"{_dateTimeService.GetUtcDateTime():dd_MM_yyyy_HH_mm_ss}.json"), messagePositions);

            var shortsPositionsToOpen = 0;
            var longsPositionsToOpen = 0;

            var availablePositionsToOpen = (options.MaximumPositions - openedPositions.Count) switch
            {
                > 0 => options.MaximumPositions - openedPositions.Count,
                _ => 0
            };

            if (availablePositionsToOpen <= 0)
            {
                _logger.LogInformation(
                    "There is no ability to open new positions. Opened positions count is: {OpenedPositionsCount}. In {Method}",
                    openedPositions.Count, nameof(GetFilteredOrdersForOpenPositionAsync));

                return new List<SignalInfo>();
            }

            _logger.LogInformation(
                "Maximum available positions for open: {Afo}. Current opened positions: {Cop}. Available positions to open: {Ato}. In {Method}",
                options.MaximumPositions, openedPositions.Count, availablePositionsToOpen,
                nameof(GetFilteredOrdersForOpenPositionAsync));

            switch (instanceResult.MarketMood)
            {
                case Mood.Short:
                    shortsPositionsToOpen = options.MaximumPositionsPerIteration;
                    break;
                case Mood.Long:
                    longsPositionsToOpen = options.MaximumPositionsPerIteration;
                    break;
                case Mood.Balanced:
                {
                    if (options.MaximumPositionsPerIteration % 2 == 0)
                    {
                        shortsPositionsToOpen = options.MaximumPositionsPerIteration / 2;
                        longsPositionsToOpen = options.MaximumPositionsPerIteration / 2;
                    }
                    else
                    {
                        var dividedPerIteration = (int)Math.Round((decimal)options.MaximumPositionsPerIteration / 2, 0);

                        if (instanceResult.ShortMarketMoodPercent > instanceResult.LongsMarketMoodPercent)
                        {
                            shortsPositionsToOpen = dividedPerIteration;
                            longsPositionsToOpen = options.MaximumPositionsPerIteration - dividedPerIteration;
                        }
                        else
                        {
                            shortsPositionsToOpen = options.MaximumPositionsPerIteration - dividedPerIteration;
                            longsPositionsToOpen = dividedPerIteration;
                        }
                    }
                    
                    break;
                }
                default:
                    _logger.LogWarning("There is no market mood. In {Method}",
                        nameof(GetFilteredOrdersForOpenPositionAsync));
                    return new List<SignalInfo>();
            }

            _logger.LogInformation(
                "Maximum positions per iteration {IterCount}. Longs to check: {LongsCount}. Shorts to check: {ShortsCount}. In {Method}",
                options.MaximumPositionsPerIteration, longsPositionsToOpen, shortsPositionsToOpen,
                nameof(GetFilteredOrdersForOpenPositionAsync));

            var shortsToOpen = GetPositions(shortsPositionsToOpen, PositionSide.Short, shortSignals, openedPositions, positionsInfo, options);
            var longsToOpen = GetPositions(longsPositionsToOpen, PositionSide.Long, longSignals, openedPositions, positionsInfo, options);

            _logger.LogInformation("Longs to open: {LongsCount}. Shorts to open: {ShortsCount}. In {Method}",
                longsToOpen.Count, shortsToOpen.Count, nameof(GetFilteredOrdersForOpenPositionAsync));

            shortsToOpen.AddRange(longsToOpen);

            return shortsToOpen;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetFilteredOrdersForOpenPositionAsync));

            return new List<SignalInfo>();
        }
    }

    public Task<bool> IsNeedToPlaceMarketAverageOrderAsync(InstanceResult instanceResult, Position openedPosition, decimal lastPrice, 
        SymbolMarketInfo symbolMarketInfo, BinanceFuturesUsdtSymbol symbolInfo, PercentLimitTradeLogicLogicOptions options)
    {
        try
        {
            if (symbolInfo.PriceFilter == null)
            {
                _logger.LogError("{Position}. {Filter} is null. In {Method}",
                    openedPosition.ToString(), nameof(symbolInfo.PriceFilter),
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (!options.EnableAveraging)
            {
                _logger.LogInformation("{Position}. Averaging is disabled. In {Method}",
                    openedPosition.ToString(), nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            var positionOption = GetPositionOption(instanceResult.MarketMood, options, FilterPositionAction.Average, 
                openedPosition.PositionSide);

            if (positionOption == null)
            {
                _logger.LogError("{Position}. Signal options  is null. In {Method}", 
                    openedPosition.ToString(), nameof(IsNeedToPlaceMarketAverageOrderAsync));
                
                return Task.FromResult(false);
            }
            
            if (positionOption.Status == PositionOptionStatus.Disabled)
            {
                _logger.LogInformation("{Position}. Signal status is Disabled. In {Method}", 
                    openedPosition.ToString(),nameof(GetFilteredOrdersForOpenPositionAsync));

                return Task.FromResult(false);
            }
            
            if (positionOption.KlinePocLocation != PocLocation.Any)
            {
                switch (symbolMarketInfo.IsPocInWick)
                {
                    case true when positionOption.KlinePocLocation != PocLocation.InWick:
                    case false when positionOption.KlinePocLocation != PocLocation.InBody:
                        _logger.LogInformation("{Position}. Is POC in wick: {IsPocInWick}. Poc location is: {PocLocation}. In {Method}",
                            openedPosition.ToString(), symbolMarketInfo.IsPocInWick, positionOption.KlinePocLocation, 
                            nameof(IsNeedToPlaceMarketAverageOrderAsync));
                    return Task.FromResult(false);
                }
            }
            
            if (positionOption.KlinePower != KlinePowerSignal.Any)
            {
                switch (symbolMarketInfo.Power)
                {
                    case KlinePower.Bear when positionOption.KlinePower != KlinePowerSignal.Bear:
                    case KlinePower.Bull when positionOption.KlinePower != KlinePowerSignal.Bull:
                        _logger.LogInformation("{Position}. Kline power is: {KlinePower}. Accepted power is: {AcceptedPower}. In {Method}",
                            openedPosition.ToString(), symbolMarketInfo.Power, positionOption.KlinePower, 
                            nameof(IsNeedToPlaceMarketAverageOrderAsync));
                        return Task.FromResult(false);
                }
            }
            
            if (positionOption.PocVolumeDeltaType != KlineDeltaType.Any)
            {
                switch (symbolMarketInfo.PocDeltaVolume)
                {
                    case > 0 when positionOption.PocVolumeDeltaType != KlineDeltaType.Buy:
                    case < 0 when positionOption.PocVolumeDeltaType != KlineDeltaType.Sell:
                        _logger.LogInformation("{Position}. Poc delta type is: {PocDeltaType}. Accepted Poc volume delta type is: {AcceptedPocDeltaType}. In {Method}",
                            openedPosition.ToString(), symbolMarketInfo.PocDeltaVolume, positionOption.PocVolumeDeltaType, nameof(IsNeedToPlaceMarketAverageOrderAsync));
                        return Task.FromResult(false);
                }
            }

            if (positionOption.KlinePocLevel != PocLevel.Any)
            {
                switch (symbolMarketInfo.KlinePocType)
                {
                    case KlinePocType.High when positionOption.KlinePocLevel != PocLevel.AtTop:
                    case KlinePocType.MiddleHigh when positionOption.KlinePocLevel != PocLevel.AtTop:
                    case KlinePocType.Middle when positionOption.KlinePocLevel != PocLevel.InMiddle:
                    case KlinePocType.MiddleLow when positionOption.KlinePocLevel != PocLevel.AtBottom:
                    case KlinePocType.Low when positionOption.KlinePocLevel != PocLevel.AtBottom:
                        _logger.LogInformation("{Position}. Kline power is: {KlinePower}. Accepted power is: {AcceptedPower}. In {Method}",
                            openedPosition.ToString(), symbolMarketInfo.Power, positionOption.KlinePower, 
                            nameof(IsNeedToPlaceMarketAverageOrderAsync));
                        return Task.FromResult(false);
                }
            }
            
            if (positionOption.MinTrades < symbolMarketInfo.KlineTotalTrades)
            {
                _logger.LogInformation("{Position}. Not valid amount of trades. Kline trades: {KlineTrades}. Accepted trades: {AcceptedTrades}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlineTotalTrades, positionOption.MinTrades, 
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }
            
            if (positionOption.CoefficientOfVolume > 0 && Math.Abs(symbolMarketInfo.KlineVolumeCoefficient) < positionOption.CoefficientOfVolume)
            {
                _logger.LogInformation("{Position}. Not valid kline volume coefficient. Kline volume coefficient is {KlineVolumeCoefficient}. " +
                                       "Accepted volume coefficient in options: {KlineVolumeCoefficientInOptions}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlineVolumeCoefficient, positionOption.CoefficientOfVolume,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (positionOption.CoefficientOfPocVolume > 0 && Math.Abs(symbolMarketInfo.PocVolumeCoefficient) < positionOption.CoefficientOfPocVolume)
            {
                _logger.LogInformation("{Position}. Not valid POC volume coefficient. POC volume coefficient is {KlineVolumeCoefficient}. " +
                                       "Accepted volume coefficient in options: {KlineVolumeCoefficientInOptions}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlineVolumeCoefficient, positionOption.CoefficientOfVolume,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }
            
            if (positionOption.CoefficientOfOrderLimits > 0 && Math.Abs(symbolMarketInfo.AsksBidsCoefficient) < positionOption.CoefficientOfOrderLimits)
            {
                _logger.LogInformation("{Position}. Not valid asks bids coefficient. Kline asks bids coefficient is {AsksBidsCoefficient}. " +
                                       "Accepted asks bids coefficient in options: {AsksBidsCoefficientInOptions}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.AsksBidsCoefficient, positionOption.CoefficientOfOrderLimits,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (positionOption.MinQuoteVolume > symbolMarketInfo.KlineQuoteVolume)
            {
                _logger.LogInformation(
                    "{Position}. Not valid trade quote volume. Kline trade asset volume is {TradeQuoteVolumeKline}. " +
                    "Accepted trade quote volume in options. {TradeQuoteVolumeInOptions}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlineQuoteVolume, positionOption.MinQuoteVolume,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (!IsKlinePocTypeValidForKlineSignalType(symbolMarketInfo.KlinePocType, openedPosition.PositionSide, positionOption.KlineSignalType))
            {
                _logger.LogInformation("{Position}. Not valid kline action. Current kline action is {KlineAction}. " +
                                       "Kline action signal for average is {KlineActionSignal}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlinePocType, positionOption.KlineSignalType, 
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (openedPosition.PositionSide == PositionSide.Short && symbolMarketInfo.TotalAsks <= symbolMarketInfo.TotalBids
                || openedPosition.PositionSide == PositionSide.Long && symbolMarketInfo.TotalBids <= symbolMarketInfo.TotalAsks)
            {
                _logger.LogInformation(
                    "{Position}. Not valid Bids and Asks coefficient. Kline side is {KlineSide}. Asks: {Asks}. Bids {Bids}. In {Method}",
                    openedPosition.ToString(), openedPosition.PositionSide, symbolMarketInfo.TotalAsks,
                    symbolMarketInfo.TotalBids, nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            var roePercent = _calculatorService.CalculateRoe(openedPosition.PositionSide, openedPosition.EntryPrice,
                lastPrice, openedPosition.Leverage);

            if (roePercent > options.AverageFromRoe)
            {
                _logger.LogInformation("{Position}. ROE percent is invalid. ROE: {Roe}%. In {Method}",
                    openedPosition.ToString(), roePercent, nameof(IsNeedToPlaceMarketAverageOrderAsync));

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

    public PercentLimitOrderToPlace IsNeedToActivateOrders(Position openedPosition, decimal lastPrice,
        PercentLimitPositionInfo percentLimitPositionInfo, BinanceFuturesAccountBalance balance, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
    {
        try
        {
            // Stop loss logic
            if (tradeLogicLogicOptions.EnableMarketStopLoss)
            {
                if (tradeLogicLogicOptions.StopLossForSide == PositionSide.Both || tradeLogicLogicOptions.StopLossForSide == openedPosition.PositionSide)
                {
                    var currentPnl = _calculatorService.CalculatePnl(openedPosition.PositionSide, lastPrice,
                        openedPosition.EntryPrice, openedPosition.TotalQuantity);
                    
                    if (currentPnl < 0 && Math.Abs(currentPnl) >= Math.Round(balance.WalletBalance * tradeLogicLogicOptions.StopLossPercentFromDeposit / 100, 2))
                    {
                        _logger.LogInformation(
                            "{Position}. Order will be closed by stop loss. Stop loss side: {Side}. Current pnl: {CurrentPnl}. " +
                            "Current wallet balance: {CurrentWalletBalance}. Percent from deposit to loss {ToLossPercent}%. In {Method}",
                            openedPosition.ToString(), tradeLogicLogicOptions.StopLossForSide, currentPnl,
                            balance.WalletBalance,
                            tradeLogicLogicOptions.StopLossPercentFromDeposit, nameof(IsNeedToActivateOrders));

                        return PercentLimitOrderToPlace.MarketToClose;
                    }
                }
            }

            var roePercent = _calculatorService.CalculateRoe(openedPosition.PositionSide, openedPosition.EntryPrice,
                lastPrice, openedPosition.Leverage);

            // When trailing stop is activated
            if (percentLimitPositionInfo.IsTrailingStopActivated)
            {
                if (roePercent > percentLimitPositionInfo.HighestRoe)
                {
                    _logger.LogInformation(
                        "{Position}. Previous roe updated. Previous ROE: {Previous}%. New ROE: {New}%. In {Method}",
                        openedPosition.ToString(), percentLimitPositionInfo.HighestRoe, roePercent,
                        nameof(IsNeedToActivateOrders));

                    percentLimitPositionInfo.HighestRoe = roePercent;

                    return PercentLimitOrderToPlace.None;
                }

                var callBackRateFromHighestRoe = percentLimitPositionInfo.HighestRoe -
                                                 tradeLogicLogicOptions.CallbackRate * openedPosition.Leverage;
                if (roePercent > callBackRateFromHighestRoe)
                {
                    return PercentLimitOrderToPlace.None;
                }

                _logger.LogInformation("{Position}. Market Close Position. ROE is valid and order will be placed. " +
                                       "Current ROE: {Roe}%. Highest ROE: {HighestRoe}%. In {Method}",
                    openedPosition.ToString(), roePercent, percentLimitPositionInfo.HighestRoe,
                    nameof(IsNeedToActivateOrders));

                return PercentLimitOrderToPlace.MarketToClose;
            }

            // Market Stop logic
            if (tradeLogicLogicOptions.EnableMarketStopToExit &&
                roePercent >= tradeLogicLogicOptions.MarketStopExitRoeActivation &&
                percentLimitPositionInfo.IsNeedToPlaceMarketStop)
            {
                // By balance percent
                if (tradeLogicLogicOptions.MarketStopExitActivationFromAvailableBalancePercent.HasValue
                    && openedPosition.InitialMargin >= balance.AvailableBalance *
                    tradeLogicLogicOptions.MarketStopExitActivationFromAvailableBalancePercent / 100)
                {
                    _logger.LogInformation(
                        "{Position}. Market Stop to close by balance. ROE is valid and order will be placed. ROE: {Roe}%. In {Method}",
                        openedPosition.ToString(), roePercent, nameof(IsNeedToActivateOrders));

                    return PercentLimitOrderToPlace.MarketStopToExit;
                }

                // By time
                if (tradeLogicLogicOptions.MarketStopExitActivationAfterTime.HasValue
                    && openedPosition.LastUpdateTime.AddMilliseconds(tradeLogicLogicOptions
                        .MarketStopExitActivationAfterTime.Value.TotalMilliseconds)
                    < _dateTimeService.GetUtcDateTime())
                {
                    var timeAfterAdding = openedPosition.LastUpdateTime.AddMilliseconds(
                        tradeLogicLogicOptions.MarketStopExitActivationAfterTime.Value.TotalMilliseconds);

                    _logger.LogInformation(
                        "{Position}. Market Stop to close by time. ROE is valid and order will be placed. " +
                        "Position LasTime: {PositionTime}. Time after adding {AfterAdding}. Utc time {UtcTime}. ROE: {Roe}%. In {Method}",
                        openedPosition.ToString(), openedPosition.LastUpdateTime, timeAfterAdding,
                        _dateTimeService.GetUtcDateTime(),
                        roePercent, nameof(IsNeedToActivateOrders));

                    return PercentLimitOrderToPlace.MarketStopToExit;
                }
            }

            // When Trailing stop is not activated
            if (tradeLogicLogicOptions.EnableTrailingStops && roePercent >= tradeLogicLogicOptions.TrailingStopRoe &&
                !percentLimitPositionInfo.IsTrailingStopActivated)
            {
                percentLimitPositionInfo.IsTrailingStopActivated = true;

                percentLimitPositionInfo.HighestRoe = roePercent;

                _logger.LogInformation("{Position}. Trailing stop check activated. ROE: {Roe}%. In {Method}",
                    openedPosition.ToString(), roePercent, nameof(IsNeedToActivateOrders));

                if (!percentLimitPositionInfo.IsNeedToPlaceMarketStop ||
                    !tradeLogicLogicOptions.MarketStopSafePriceFromLastPricePercent.HasValue)
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

    private List<SymbolMarketInfo> GetShortSignalsForOpen(InstanceResult instanceResult, PercentLimitTradeLogicLogicOptions options)
    {
        var positionOption = GetPositionOption(instanceResult.MarketMood, options, FilterPositionAction.Open, PositionSide.Short);
        
        if (positionOption == null)
        {
            _logger.LogError("Signal options for side: {PositionSide}, action: {PositionAction} is null. In {Method}", 
                PositionSide.Short, FilterPositionAction.Open, nameof(GetFilteredOrdersForOpenPositionAsync));
                
            return new List<SymbolMarketInfo>();
        }
            
        if (positionOption.Status == PositionOptionStatus.Disabled)
        {
            _logger.LogInformation("Signal status for side: {PositionSide}, action: {PositionAction} is Disabled. In {Method}", 
                PositionSide.Short, FilterPositionAction.Open, nameof(GetFilteredOrdersForOpenPositionAsync));

            return new List<SymbolMarketInfo>();
        }
        
        var shortSignals = GetFilteredSignals(positionOption, instanceResult.Signals)
            .Where(x => IsKlinePocTypeValidForKlineSignalType(x.KlinePocType, PositionSide.Short, positionOption.KlineSignalType))
            .Where(x => x.TotalAsks > x.TotalBids)
            .OrderByDescending(x => x.KlineQuoteVolume)
            .ThenByDescending(x => x.AsksBidsCoefficient)
            .ToList();
            
        return shortSignals;
    }
    
    private List<SymbolMarketInfo> GetLongSignalsForOpen(InstanceResult instanceResult, PercentLimitTradeLogicLogicOptions options)
    {
        var positionOption = GetPositionOption(instanceResult.MarketMood, options, FilterPositionAction.Open, PositionSide.Long);
        
        if (positionOption == null)
        {
            _logger.LogError("Signal options for side: {PositionSide}, action: {PositionAction} is null. In {Method}", 
                PositionSide.Long, FilterPositionAction.Open, nameof(GetFilteredOrdersForOpenPositionAsync));
                
            return new List<SymbolMarketInfo>();
        }
            
        if (positionOption.Status == PositionOptionStatus.Disabled)
        {
            _logger.LogInformation("Signal status for side: {PositionSide}, action: {PositionAction} is Disabled. In {Method}", 
                PositionSide.Long, FilterPositionAction.Open, nameof(GetFilteredOrdersForOpenPositionAsync));

            return new List<SymbolMarketInfo>();
        }

        var longSignals = GetFilteredSignals(positionOption, instanceResult.Signals)
            .Where(x => IsKlinePocTypeValidForKlineSignalType(x.KlinePocType, PositionSide.Long, positionOption.KlineSignalType))
            .Where(x => x.TotalAsks < x.TotalBids)
            .OrderByDescending(x => x.KlineQuoteVolume)
            .ThenByDescending(x => x.AsksBidsCoefficient)
            .ToList();
            
        return longSignals;
    }

    private static IEnumerable<SymbolMarketInfo> GetFilteredSignals(PositionOption positionOption, IEnumerable<SymbolMarketInfo> signals)
    {
        var filteredSignals = signals
            .WhereIf(positionOption.KlinePocLocation == PocLocation.InBody, x => !x.IsPocInWick)
            .WhereIf(positionOption.KlinePocLocation == PocLocation.InWick, x => x.IsPocInWick)
            .WhereIf(positionOption.KlinePower == KlinePowerSignal.Bull, x => x.Power == KlinePower.Bull)
            .WhereIf(positionOption.KlinePower == KlinePowerSignal.Bear, x => x.Power == KlinePower.Bear)
            .WhereIf(positionOption.PocVolumeDeltaType == KlineDeltaType.Buy, x => x.PocDeltaVolume > 0)
            .WhereIf(positionOption.PocVolumeDeltaType == KlineDeltaType.Sell, x => x.PocDeltaVolume < 0)
            .WhereIf(positionOption.KlinePocLevel == PocLevel.InMiddle, x => x.KlinePocType == KlinePocType.Middle)
            .WhereIf(positionOption.KlinePocLevel == PocLevel.AtTop, x => x.KlinePocType is KlinePocType.MiddleHigh or KlinePocType.High)
            .WhereIf(positionOption.KlinePocLevel == PocLevel.AtBottom, x => x.KlinePocType is KlinePocType.MiddleLow or KlinePocType.Low)
            .WhereIf(positionOption.CoefficientOfVolume > 0, x => Math.Abs(x.KlineVolumeCoefficient) >= positionOption.CoefficientOfVolume)
            .WhereIf(positionOption.CoefficientOfPocVolume > 0, x => Math.Abs(x.PocVolumeCoefficient) >= positionOption.CoefficientOfPocVolume)
            .WhereIf(positionOption.CoefficientOfOrderLimits > 0, x => Math.Abs(x.AsksBidsCoefficient) >= positionOption.CoefficientOfOrderLimits)
            .Where(x => x.KlineTotalTrades >= positionOption.MinTrades)
            .Where(x => x.KlineQuoteVolume >= positionOption.MinQuoteVolume);

        return filteredSignals;
    }
    
    private List<SignalInfo> GetPositions(int toOpen, PositionSide positionSide, IEnumerable<SymbolMarketInfo> symbolMarketInfos,
        IReadOnlyCollection<Position> openedPositions, IReadOnlyCollection<BinancePositionDetailsUsdt> positionsInfo,
        PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
    {
        if (toOpen == 0)
        {
            return new List<SignalInfo>();
        }

        var counter = 0;
        var list = new List<SignalInfo>();
        foreach (var symbolMarketInfo in symbolMarketInfos)
        {
            if (counter >= toOpen)
            {
                break;
            }

            if (openedPositions.Any(x => x.Name == symbolMarketInfo.FuturesUsdName && x.PositionSide == positionSide))
            {
                _logger.LogInformation("{SymbolName} | {Side}. Same position is already exist. In {Method}",
                    symbolMarketInfo.FuturesUsdName, positionSide, nameof(GetPositions));
                
                continue;
            }

            if (positionsInfo.Any(x => x.Symbol == symbolMarketInfo.FuturesUsdName
                                       && (x.Leverage != tradeLogicLogicOptions.Leverage ||
                                           x.MarginType != tradeLogicLogicOptions.MarginType)))
            {
                _logger.LogWarning("{SymbolName} | {Side}. Skip to open position because leverage or margin types are different. In {Method}",
                    symbolMarketInfo.FuturesUsdName, positionSide, nameof(GetPositions));

                continue;
            }

            list.Add(new SignalInfo(symbolMarketInfo.FuturesUsdName, symbolMarketInfo.QuoteAsset, positionSide));

            counter++;
        }

        return list;
    }

    private static bool IsKlinePocTypeValidForKlineSignalType(KlinePocType klinePocType, PositionSide positionSide, KlineSignalType klineSignalType)
    {
        switch (positionSide)
        {
            case PositionSide.Short:
                switch (klineSignalType)
                {
                    case KlineSignalType.Low when klinePocType is KlinePocType.High or KlinePocType.MiddleHigh or KlinePocType.Middle:
                    case KlineSignalType.Middle when klinePocType is KlinePocType.High or KlinePocType.MiddleHigh:
                    case KlineSignalType.Strong when klinePocType is KlinePocType.High:
                        return true;
                    default:
                        return false;
                }
            case PositionSide.Long:
                switch (klineSignalType)
                {
                    case KlineSignalType.Low when klinePocType is KlinePocType.Low or KlinePocType.MiddleLow or KlinePocType.Middle:
                    case KlineSignalType.Middle when klinePocType is KlinePocType.Low or KlinePocType.MiddleLow:
                    case KlineSignalType.Strong when klinePocType is KlinePocType.Low:
                        return true;
                    default:
                        return false;
                }
            case PositionSide.Both:
            default:
                return false;
        }
    }

    private static PositionOption? GetPositionOption(Mood marketMood, PercentLimitTradeLogicLogicOptions options, FilterPositionAction filterPositionAction, PositionSide positionSide)
    {
        switch (filterPositionAction)
        {
            case FilterPositionAction.Open:
                switch (marketMood)
                {
                    case Mood.Short:
                        switch (positionSide)
                        {
                            case PositionSide.Short:
                                return options.SignalSettingsForOpen.ShortMarketMood.Short;
                            case PositionSide.Long:
                                return options.SignalSettingsForOpen.ShortMarketMood.Long;
                        }
                        break;
                    case Mood.Long:
                        switch (positionSide)
                        {
                            case PositionSide.Short:
                                return options.SignalSettingsForOpen.LongMarketMood.Short;
                            case PositionSide.Long:
                                return options.SignalSettingsForOpen.LongMarketMood.Long;
                        }
                        break;
                    case Mood.Balanced:
                        switch (positionSide)
                        {
                            case PositionSide.Short:
                                return options.SignalSettingsForOpen.BalancedMarketMood.Short;
                            case PositionSide.Long:
                                return options.SignalSettingsForOpen.BalancedMarketMood.Long;
                        }
                        break;
                }
                break;
            case FilterPositionAction.Average:
                switch (marketMood)
                {
                    case Mood.Short:
                        switch (positionSide)
                        {
                            case PositionSide.Short:
                                return options.SignalSettingsForAverage.ShortMarketMood.Short;
                            case PositionSide.Long:
                                return options.SignalSettingsForAverage.ShortMarketMood.Long;
                        }
                        break;
                    case Mood.Long:
                        switch (positionSide)
                        {
                            case PositionSide.Short:
                                return options.SignalSettingsForAverage.LongMarketMood.Short;
                            case PositionSide.Long:
                                return options.SignalSettingsForAverage.LongMarketMood.Long;
                        }
                        break;
                    case Mood.Balanced:
                        switch (positionSide)
                        {
                            case PositionSide.Short:
                                return options.SignalSettingsForAverage.BalancedMarketMood.Short;
                            case PositionSide.Long:
                                return options.SignalSettingsForAverage.BalancedMarketMood.Long;
                        }
                        break;
                }
                break;
        }

        return null;
    }

    #endregion
}