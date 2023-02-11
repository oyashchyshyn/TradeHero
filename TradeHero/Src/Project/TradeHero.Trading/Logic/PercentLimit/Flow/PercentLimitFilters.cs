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

    public async Task<List<SymbolMarketInfo>> GetFilteredOrdersForOpenPositionAsync(InstanceResult instanceResult,
        PercentLimitTradeLogicLogicOptions options, IReadOnlyCollection<Position> openedPositions,
        IReadOnlyCollection<BinancePositionDetailsUsdt> positionsInfo)
    {
        try
        {
            var topShortKlines = instanceResult.ShortSignals
                .WhereIf(options.IsPocMustBeInWickForOpen, x => x.IsPocInWick)
                .WhereIf(options.CoefficientOfSellBuyVolumeForOpen > 0,
                    x => x.KlineVolumeCoefficient > 0 &&
                         x.KlineVolumeCoefficient >= options.CoefficientOfSellBuyVolumeForOpen)
                .WhereIf(options.CoefficientOfBidAsksForOpen > 0,
                    x => x.AsksBidsCoefficient > 0 && x.AsksBidsCoefficient >= options.CoefficientOfBidAsksForOpen)
                .Where(x => GetKlineActionsFromKlineActionSignal(options.KlineActionForOpen, x.PositionSide)
                    .Contains(x.KlineAction))
                .Where(x => GetKlinePowersFromKlinePowerSignal(options.KlinePowerForOpen, x.PositionSide)
                    .Contains(x.Power))
                .Where(x => x.KlineTradedQuoteVolume >= options.MinQuoteVolumeForOpen)
                .Where(x => x.TotalAsks > x.TotalBids)
                .OrderByDescending(x => x.AsksBidsCoefficient)
                .ThenByDescending(x => x.PocTradesCoefficient)
                .ToArray();

            var topLongKlines = instanceResult.LongSignals
                .WhereIf(options.IsPocMustBeInWickForOpen, x => x.IsPocInWick)
                .WhereIf(options.CoefficientOfSellBuyVolumeForOpen > 0,
                    x => x.KlineVolumeCoefficient < 0 &&
                         Math.Abs(x.KlineVolumeCoefficient) >= options.CoefficientOfSellBuyVolumeForOpen)
                .WhereIf(options.CoefficientOfBidAsksForOpen > 0,
                    x => x.AsksBidsCoefficient < 0 &&
                         Math.Abs(x.AsksBidsCoefficient) >= options.CoefficientOfBidAsksForOpen)
                .Where(x => GetKlineActionsFromKlineActionSignal(options.KlineActionForOpen, x.PositionSide)
                    .Contains(x.KlineAction))
                .Where(x => GetKlinePowersFromKlinePowerSignal(options.KlinePowerForOpen, x.PositionSide)
                    .Contains(x.Power))
                .Where(x => x.KlineTradedQuoteVolume >= options.MinQuoteVolumeForOpen)
                .Where(x => x.TotalBids > x.TotalAsks)
                .OrderByDescending(x => x.AsksBidsCoefficient)
                .ThenByDescending(x => x.PocTradesCoefficient)
                .ToArray();

            _logger.LogInformation(
                "Filtered Longs: {FilteredLongsCount}. Filtered Shorts: {FilteredShortsCount}. In {Method}",
                topLongKlines.Length, topShortKlines.Length, nameof(GetFilteredOrdersForOpenPositionAsync));

            var folderName = Path.Combine(_environmentService.GetBasePath(), FolderConstants.ClusterResultsFolder);
            var jsonShorts = _jsonService.SerializeObject(instanceResult.ShortSignals, Formatting.Indented).Data;
            var jsonLongs = _jsonService.SerializeObject(instanceResult.LongSignals, Formatting.Indented).Data;
            var jsonFilteredShorts = _jsonService.SerializeObject(topShortKlines, Formatting.Indented).Data;
            var jsonFilteredLongs = _jsonService.SerializeObject(topLongKlines, Formatting.Indented).Data;

            var messagePositions =
                $"SHORTS:{Environment.NewLine}{jsonShorts}{Environment.NewLine}LONGS:{Environment.NewLine}{jsonLongs}";
            var messagePositionsToOpen =
                $"SHORTS:{Environment.NewLine}{jsonFilteredShorts}{Environment.NewLine}LONGS:{Environment.NewLine}{jsonFilteredLongs}";

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

                return new List<SymbolMarketInfo>();
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
                    switch (instanceResult.SignalsMood)
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
                                var dividedPerIteration =
                                    (int)Math.Round((decimal)options.MaximumPositionsPerIteration / 2, 0);

                                if (instanceResult.ShortSignalsCount > instanceResult.LongSignalsCount)
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
                            _logger.LogWarning("There is no signal mood. In {Method}",
                                nameof(GetFilteredOrdersForOpenPositionAsync));
                            return new List<SymbolMarketInfo>();
                    }

                    break;
                default:
                    _logger.LogWarning("There is no market mood. In {Method}",
                        nameof(GetFilteredOrdersForOpenPositionAsync));
                    return new List<SymbolMarketInfo>();
            }

            _logger.LogInformation(
                "Maximum positions per iteration {IterCount}. Longs to check: {LongsCount}. Shorts to check: {ShortsCount}. In {Method}",
                options.MaximumPositionsPerIteration, longsPositionsToOpen, shortsPositionsToOpen,
                nameof(GetFilteredOrdersForOpenPositionAsync));

            var shortsToOpen = GetPositions(shortsPositionsToOpen, topShortKlines, openedPositions, positionsInfo,
                options);
            var longsToOpen =
                GetPositions(longsPositionsToOpen, topLongKlines, openedPositions, positionsInfo, options);

            _logger.LogInformation("Longs to open: {LongsCount}. Shorts to open: {ShortsCount}. In {Method}",
                longsToOpen.Count, shortsToOpen.Count, nameof(GetFilteredOrdersForOpenPositionAsync));

            shortsToOpen.AddRange(longsToOpen);

            return shortsToOpen;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetFilteredOrdersForOpenPositionAsync));

            return new List<SymbolMarketInfo>();
        }
    }

    public Task<bool> IsNeedToPlaceMarketAverageOrderAsync(InstanceResult instanceResult, Position openedPosition,
        decimal lastPrice, SymbolMarketInfo symbolMarketInfo,
        BinanceFuturesUsdtSymbol symbolInfo, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
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

            if (!tradeLogicLogicOptions.EnableAveraging)
            {
                _logger.LogInformation("{Position}. Averaging is disabled. In {Method}",
                    openedPosition.ToString(), nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (openedPosition.PositionSide != symbolMarketInfo.PositionSide)
            {
                _logger.LogInformation("{Position}. Not valid side for average. Kline side is {KlineSide}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.PositionSide,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (tradeLogicLogicOptions.IsPocMustBeInWickForAverage && !symbolMarketInfo.IsPocInWick)
            {
                _logger.LogInformation("{Position}. Poc does not located in wick of kline. In {Method}",
                    openedPosition.ToString(), nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (tradeLogicLogicOptions.CoefficientOfSellBuyVolumeForAverage > 0 
                && IsCoefficientValid(openedPosition.PositionSide, symbolMarketInfo.KlineVolumeCoefficient, tradeLogicLogicOptions.CoefficientOfSellBuyVolumeForAverage))
            {
                _logger.LogInformation("{Position}. Not valid volume coefficient. Kline volume coefficient is {KlineVolumeCoefficient}. " +
                                       "Accepted volume coefficient in options: {KlineVolumeCoefficientInOptions}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlineVolumeCoefficient, tradeLogicLogicOptions.CoefficientOfSellBuyVolumeForAverage,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (tradeLogicLogicOptions.CoefficientOfBidAsksForAverage > 0 
                && IsCoefficientValid(openedPosition.PositionSide, symbolMarketInfo.AsksBidsCoefficient, tradeLogicLogicOptions.CoefficientOfBidAsksForAverage))
            {
                _logger.LogInformation("{Position}. Not valid asks bids coefficient. Kline asks bids coefficient is {AsksBidsCoefficient}. " +
                                       "Accepted asks bids coefficient in options: {AsksBidsCoefficientInOptions}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.AsksBidsCoefficient, tradeLogicLogicOptions.CoefficientOfBidAsksForAverage,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (tradeLogicLogicOptions.MinQuoteVolumeForAverage > symbolMarketInfo.KlineTradedQuoteVolume)
            {
                _logger.LogInformation(
                    "{Position}. Not valid trade quote volume. Kline trade asset volume is {TradeQuoteVolumeKline}. " +
                    "Accepted trade quote volume in options. {TradeQuoteVolumeInOptions}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlineTradedQuoteVolume,
                    tradeLogicLogicOptions.MinQuoteVolumeForAverage,
                    nameof(IsNeedToPlaceMarketAverageOrderAsync));

                return Task.FromResult(false);
            }

            if (!GetKlineActionsFromKlineActionSignal(tradeLogicLogicOptions.KlineActionForAverage, openedPosition.PositionSide).Contains(symbolMarketInfo.KlineAction))
            {
                _logger.LogInformation("{Position}. Not valid kline action. Current kline action is {KlineAction}. " +
                                       "Kline action signal for average is {KlineActionSignal}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.KlineAction,
                    tradeLogicLogicOptions.KlineActionForAverage,
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

            if (symbolMarketInfo.PositionSide == PositionSide.Short && symbolMarketInfo.TotalAsks <= symbolMarketInfo.TotalBids
                || symbolMarketInfo.PositionSide == PositionSide.Long && symbolMarketInfo.TotalBids <= symbolMarketInfo.TotalAsks)
            {
                _logger.LogInformation(
                    "{Position}. Not valid Bids and Asks coefficient. Kline side is {KlineSide}. Asks: {Asks}. Bids {Bids}. In {Method}",
                    openedPosition.ToString(), symbolMarketInfo.PositionSide, symbolMarketInfo.TotalAsks,
                    symbolMarketInfo.TotalBids, nameof(IsNeedToPlaceMarketAverageOrderAsync));

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
                case Mood.Short when openedPosition.PositionSide == PositionSide.Long:
                case Mood.Long when openedPosition.PositionSide == PositionSide.Short:
                case Mood.Balanced:
                    _logger.LogInformation(
                        "{Position}. Wrong market mood. Current market mood is {MarketMood}. In {Method}",
                        openedPosition.ToString(), instanceResult.MarketMood,
                        nameof(IsNeedToPlaceMarketAverageOrderAsync));
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
        PercentLimitPositionInfo percentLimitPositionInfo,
        BinanceFuturesAccountBalance balance, PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
    {
        try
        {
            // Stop loss logic
            if (tradeLogicLogicOptions.EnableMarketStopLoss)
            {
                var currentPnl = _calculatorService.CalculatePnl(openedPosition.PositionSide, lastPrice,
                    openedPosition.EntryPrice, openedPosition.TotalQuantity);

                if (tradeLogicLogicOptions.StopLossForSide == PositionSide.Both ||
                    tradeLogicLogicOptions.StopLossForSide == openedPosition.PositionSide)
                {
                    if (Math.Abs(currentPnl) >=
                        Math.Round(balance.WalletBalance * tradeLogicLogicOptions.StopLossPercentFromDeposit / 100, 2))
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

    private List<SymbolMarketInfo> GetPositions(int toOpen, IEnumerable<SymbolMarketInfo> klineInfos,
        IReadOnlyCollection<Position> openedPositions,
        IReadOnlyCollection<BinancePositionDetailsUsdt> positionsInfo,
        PercentLimitTradeLogicLogicOptions tradeLogicLogicOptions)
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

            if (openedPositions.Any(x => x.Name == position.FuturesUsdName && x.PositionSide == position.PositionSide))
            {
                continue;
            }

            if (positionsInfo.Any(x => x.Symbol == position.FuturesUsdName
                                       && (x.Leverage != tradeLogicLogicOptions.Leverage ||
                                           x.MarginType != tradeLogicLogicOptions.MarginType)))
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

    private static List<KlineAction> GetKlineActionsFromKlineActionSignal(KlineActionSignal klineActionSignal,
        PositionSide positionSide)
    {
        return klineActionSignal switch
        {
            KlineActionSignal.Low when positionSide == PositionSide.Short => new List<KlineAction>
            {
                KlineAction.StopSlow, KlineAction.None, KlineAction.StopStrong
            },
            KlineActionSignal.Low when positionSide == PositionSide.Long => new List<KlineAction>
            {
                KlineAction.PushSlow, KlineAction.None, KlineAction.PushStrong
            },
            KlineActionSignal.Middle when positionSide == PositionSide.Short => new List<KlineAction>
            {
                KlineAction.StopSlow, KlineAction.StopStrong
            },
            KlineActionSignal.Middle when positionSide == PositionSide.Long => new List<KlineAction>
            {
                KlineAction.PushSlow, KlineAction.PushStrong
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

    private static List<KlinePower> GetKlinePowersFromKlinePowerSignal(KlinePowerSignal klinePowerSignal,
        PositionSide positionSide)
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

    private static bool IsCoefficientValid(PositionSide positionSide, decimal coefficient, decimal optionsCoefficient)
    {
        return positionSide switch
        {
            PositionSide.Long when coefficient < 0 && Math.Abs(coefficient) >= optionsCoefficient => true,
            PositionSide.Short when coefficient > 0 && coefficient >= optionsCoefficient => true,
            _ => false
        };
    }

    #endregion
}