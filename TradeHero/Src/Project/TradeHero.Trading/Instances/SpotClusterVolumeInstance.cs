using System.Diagnostics;
using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Client.Models;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Trading;
using TradeHero.Contracts.Trading.Models.Instance;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Core.Extensions;
using TradeHero.Core.Models;
using TradeHero.Trading.Instances.Models;
using TradeHero.Trading.Instances.Options;

namespace TradeHero.Trading.Instances;

internal class SpotClusterVolumeInstance : IInstance
{
    private readonly ILogger<SpotClusterVolumeInstance> _logger;
    private readonly IThRestBinanceClient _restBinanceClient;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICalculatorService _calculatorService;

    public SpotClusterVolumeInstance(
        ILogger<SpotClusterVolumeInstance> logger,
        IThRestBinanceClient restBinanceClient,
        IDateTimeService dateTimeService, 
        ICalculatorService calculatorService
        )
    {
        _logger = logger;
        _restBinanceClient = restBinanceClient;
        _dateTimeService = dateTimeService;
        _calculatorService = calculatorService;
    }

    public async Task<GenericBaseResult<InstanceResult>> GenerateInstanceResultAsync(ITradeLogicStore store, BaseInstanceOptions instanceOptions, 
        CancellationToken cancellationToken)
    {
        try
        {
            var localInstanceOptions = (SpotClusterVolumeOptions)instanceOptions;
            
            var futuresUsdSymbolsInfo = store.FuturesUsd.ExchangerData.ExchangeInfo.Symbols
                .Where(x => x.ContractType is ContractType.Perpetual)
                .Where(x => x.Status == SymbolStatus.Trading);

            var symbolsMarketInfos = new List<SymbolMarketInfo>();
            foreach (var futuresUsdSymbolInfo in futuresUsdSymbolsInfo)
            {
                var spotSymbolInfo = store.Spot.ExchangerData.ExchangeInfo.Symbols
                    .Where(x => x.QuoteAsset == futuresUsdSymbolInfo.QuoteAsset)
                    .Where(x => futuresUsdSymbolInfo.BaseAsset == x.BaseAsset || futuresUsdSymbolInfo.BaseAsset == $"1000{x.BaseAsset}")
                    .FirstOrDefault(x => x.Status == SymbolStatus.Trading);

                if (spotSymbolInfo == null)
                {
                    continue;
                }
                
                symbolsMarketInfos.Add(new SymbolMarketInfo
                {
                    SpotName = spotSymbolInfo.Name,
                    FuturesUsdName = futuresUsdSymbolInfo.Name,
                    BaseFuturesUsdAsset = futuresUsdSymbolInfo.BaseAsset,
                    QuoteAsset = futuresUsdSymbolInfo.QuoteAsset
                });
            }
            
            if (!symbolsMarketInfos.Any())
            {
                _logger.LogWarning("Combination of spot and futures-usd symbols info are empty. In {Method}", 
                    nameof(GenerateInstanceResultAsync));

                return new GenericBaseResult<InstanceResult>(ActionResult.Error);
            }

            var tickersRequest = await _restBinanceClient.SpotApi.ExchangeData.GetTickersAsync(
                ct:cancellationToken
            );

            if (!tickersRequest.Success)
            {
                _logger.LogWarning(new ThException(tickersRequest.Error), "In {Method}", 
                    nameof(GenerateInstanceResultAsync));

                return new GenericBaseResult<InstanceResult>(ActionResult.Error);
            }

            var spotSymbolNames = symbolsMarketInfos.Select(x => x.SpotName)
                .ToArray();

            var filteredTickersData = tickersRequest.Data.Where(x => spotSymbolNames.Contains(x.Symbol))
                .ToArray();

            var totalCount = (decimal)filteredTickersData.Length;
            var priceChangedToPlusCount = (decimal)filteredTickersData.Count(x => x.PriceChangePercent > 0);
            var longMoodPercent = Math.Round(priceChangedToPlusCount / totalCount * 100m, 2);
            var shortMoodPercent = Math.Round(100m - longMoodPercent, 2);

            var instanceResult = new InstanceResult
            {
                Interval = localInstanceOptions.Interval,
                Market = localInstanceOptions.Market,
                Side = localInstanceOptions.Side,
                LongsMarketMoodPercent = longMoodPercent,
                ShortMarketMoodPercent = shortMoodPercent,
                MarketMood = shortMoodPercent >= 60 ? MarketMood.Short : longMoodPercent >= 60 ? MarketMood.Long : MarketMood.Balanced
            };

            var filteredSymbolMarketInfos = symbolsMarketInfos
                .WhereIf(localInstanceOptions.QuoteAssets.Any(), x => localInstanceOptions.QuoteAssets.Contains(x.QuoteAsset))
                .WhereIf(localInstanceOptions.BaseAssets.Any(), x => localInstanceOptions.BaseAssets.Contains(x.BaseFuturesUsdAsset))
                .WhereIf(localInstanceOptions.ExcludeAssets.Any(), x => !localInstanceOptions.ExcludeAssets.Contains(x.BaseFuturesUsdAsset))
                .ToArray();
            
            var seconds = (int)instanceResult.Interval * localInstanceOptions.VolumeAverage;
            instanceResult.EndTo = _dateTimeService.GetUtcDateTime().AddSeconds(-(int)instanceResult.Interval);
            instanceResult.StartFrom = instanceResult.EndTo.AddSeconds(-seconds);

            var listOfIterations = _calculatorService.GetIterationValues(filteredSymbolMarketInfos.Length, localInstanceOptions.ItemsInTask);
            var groupsOfListsKlineInfo = new List<IEnumerable<SymbolMarketInfo>>();
            
            var counter = 0;
            foreach (var iteration in listOfIterations)
            {
                var klineInfosContainer = filteredSymbolMarketInfos.Skip(counter * localInstanceOptions.ItemsInTask)
                    .Take(iteration);
                
                groupsOfListsKlineInfo.Add(klineInfosContainer);
                counter++;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("CancellationToken is requested. In {Method}",
                    nameof(GenerateInstanceResultAsync));

                return new GenericBaseResult<InstanceResult>(ActionResult.CancellationTokenRequested);
            }
            
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = groupsOfListsKlineInfo.Count,
                CancellationToken = cancellationToken
            };
            
            await Parallel.ForEachAsync(groupsOfListsKlineInfo, parallelOptions, async (symbolMarketInfos, parallelCancellationToken) =>
            {
                foreach (var symbolMarketInfo in symbolMarketInfos)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("CancellationToken is requested. In {Method}",
                            nameof(Parallel.ForEachAsync));

                        return;
                    }
                    
                    await SetKlineDetailInfoAsync(
                        symbolMarketInfo,
                        localInstanceOptions.VolumeAverage,
                        localInstanceOptions.OrderBookDepthPercent,
                        instanceResult.Interval,
                        instanceResult.StartFrom,
                        instanceResult.EndTo,
                        instanceResult.Market,
                        parallelCancellationToken
                    );

                    if (symbolMarketInfo.IsAvailableForTrade)
                    {
                        switch (symbolMarketInfo.PositionSide)
                        {
                            case PositionSide.Short when instanceResult.Side is PositionSide.Both or PositionSide.Short:
                                instanceResult.ShortSignals.Add(symbolMarketInfo);
                                break;
                            case PositionSide.Long when instanceResult.Side is PositionSide.Both or PositionSide.Long:
                                instanceResult.LongSignals.Add(symbolMarketInfo);
                                break;
                            case PositionSide.Both:
                            default:
                                continue;
                        }   
                    }
                }
            });

            return new GenericBaseResult<InstanceResult>(instanceResult);
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(GenerateInstanceResultAsync));
            
            return new GenericBaseResult<InstanceResult>(ActionResult.CancellationTokenRequested);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "Error in {Method}", nameof(GenerateInstanceResultAsync));
            
            return new GenericBaseResult<InstanceResult>(ActionResult.SystemError);
        }
    }

    #region Private methods
    
    private async Task SetKlineDetailInfoAsync(SymbolMarketInfo symbolMarketInfo, int volumeMovingAverage, decimal orderBookDepthPercent, 
        KlineInterval interval, DateTime startFrom, DateTime endTo, Market market, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        
        try
        {
            stopwatch.Start();
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("CancellationToken is requested. In {Method}",
                    nameof(SetKlineDetailInfoAsync));

                return;
            }

            var klines = await _restBinanceClient.CustomRestApi.Kline.GetKlineByDateRangeAsync(
                symbolMarketInfo.SpotName,
                interval,
                startFrom,
                endTo,
                market,
                cancellationToken
            );

            if (!klines.Success)
            {
                _logger.LogWarning(new ThException(klines.Error), "{Symbol}. In {Method}", 
                    symbolMarketInfo.FuturesUsdName, nameof(SetKlineDetailInfoAsync));

                return;
            }

            if (!klines.Data.Any())
            {
                _logger.LogWarning("{Symbol}. There is no klines in request. In {Method}",
                    symbolMarketInfo.FuturesUsdName, nameof(SetKlineDetailInfoAsync));

                return;
            }

            var kline = klines.Data.Last();

            if (volumeMovingAverage > 1)
            {
                var averageKlinesVolume = klines.Data.Average(x => x.Volume);
                if (kline.Volume < averageKlinesVolume)
                {
                    return;
                }
            }

            var previousKlineClusterVolumeRequest = await _restBinanceClient.CustomRestApi.Volume.GetClusterVolumeAsync(
                symbolMarketInfo.SpotName,
                market,
                kline.OpenTime,
                kline.CloseTime,
                cancellationToken
            );

            if (!previousKlineClusterVolumeRequest.Success)
            {
                _logger.LogWarning(new ThException(previousKlineClusterVolumeRequest.Error), "{Symbol}. In {Method}", 
                    symbolMarketInfo.FuturesUsdName, nameof(SetKlineDetailInfoAsync));

                return;
            }

            if (!previousKlineClusterVolumeRequest.Data.Any())
            {
                _logger.LogInformation("{Symbol}. There is no cluster volumes in request. In {Method}",
                    symbolMarketInfo.FuturesUsdName, nameof(SetKlineDetailInfoAsync));

                return;
            }

            const int tradeRangeStep = 5;
            
            if (previousKlineClusterVolumeRequest.Data.Count < tradeRangeStep)
            {
                _logger.LogInformation("{Symbol}. Too low trades in kline. Trades count: {TradesCount}. In {Method}",
                    symbolMarketInfo.FuturesUsdName, previousKlineClusterVolumeRequest.Data.Count, 
                    nameof(SetKlineDetailInfoAsync));

                return;
            }

            var tradedRanges = GetTradedRanges(previousKlineClusterVolumeRequest.Data, tradeRangeStep);
            
            symbolMarketInfo.Power = kline.OpenPrice < kline.ClosePrice ? KlinePower.Bull : KlinePower.Bear;
            symbolMarketInfo.KlineAveragePrice = (kline.LowPrice + kline.HighPrice) / 2;

            var currentPoc = tradedRanges.MaxBy(x => x.SellVolume + x.BuyVolume);
            if (currentPoc == null)
            {
                _logger.LogInformation("{Symbol}. There is no POC in tradedRanges. In {Method}",
                    symbolMarketInfo.FuturesUsdName, nameof(SetKlineDetailInfoAsync));

                return;
            }

            symbolMarketInfo.IsPocInWick =
                (currentPoc.StartPrice > kline.OpenPrice && currentPoc.StartPrice > kline.ClosePrice)
                || (currentPoc.EndPrice < kline.OpenPrice && currentPoc.EndPrice < kline.ClosePrice);
            symbolMarketInfo.PocBuyVolume = currentPoc.BuyVolume;
            symbolMarketInfo.PocSellVolume = currentPoc.SellVolume;
            symbolMarketInfo.PocBuyTrades = currentPoc.BuyTrades;
            symbolMarketInfo.PocSellTrades = currentPoc.SellTrades;
            symbolMarketInfo.PocAveragePrice = (currentPoc.StartPrice + currentPoc.EndPrice) / 2;
            symbolMarketInfo.KlineBuyVolume = previousKlineClusterVolumeRequest.Data.Sum(x => x.BuyVolume);
            symbolMarketInfo.KlineSellVolume = previousKlineClusterVolumeRequest.Data.Sum(x => x.SellVolume);

            switch (currentPoc.Index)
            {
                case 1:
                    symbolMarketInfo.KlineAction = KlineAction.StopStrong;
                    symbolMarketInfo.PositionSide = PositionSide.Short;
                    break;
                case 2:
                    symbolMarketInfo.KlineAction = KlineAction.StopSlow;
                    symbolMarketInfo.PositionSide = PositionSide.Short;
                    break;
                case 3:
                    symbolMarketInfo.KlineAction = KlineAction.None;
                    symbolMarketInfo.PositionSide = PositionSide.Both;
                    break;
                case 4:
                    symbolMarketInfo.KlineAction = KlineAction.PushSlow;
                    symbolMarketInfo.PositionSide = PositionSide.Long;
                    break;
                case 5:
                    symbolMarketInfo.KlineAction = KlineAction.PushStrong;
                    symbolMarketInfo.PositionSide = PositionSide.Long;
                    break;
            }

            var orderBookRequest = await _restBinanceClient.SpotApi.ExchangeData.GetOrderBookAsync(
                symbolMarketInfo.SpotName, 
                ApiConstants.OrderBookItemsWithMinimalWeightAndMaximumCount,
                cancellationToken
            );
            
            if (!orderBookRequest.Success)
            {
                _logger.LogWarning(new ThException(orderBookRequest.Error), "In {Method}", 
                    nameof(SetKlineDetailInfoAsync));
            }
            else
            {
                symbolMarketInfo.Asks.AddRange(GetOrderBookDepth(orderBookRequest.Data.Asks, orderBookDepthPercent, true));
                symbolMarketInfo.Bids.AddRange(GetOrderBookDepth(orderBookRequest.Data.Bids, orderBookDepthPercent, false));
            }

            _logger.LogInformation("{Symbol}. Finished in {Elapsed}. In {Method}",
                symbolMarketInfo.FuturesUsdName, stopwatch.Elapsed, nameof(SetKlineDetailInfoAsync));
            
            stopwatch.Stop();

            symbolMarketInfo.IsAvailableForTrade = true;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Symbol}. {Message}. In {Method}",
                symbolMarketInfo.FuturesUsdName, taskCanceledException.Message, nameof(SetKlineDetailInfoAsync));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Symbol}. In {Method}", 
                symbolMarketInfo.FuturesUsdName, nameof(SetKlineDetailInfoAsync));
        }
    }
    
    private static IEnumerable<BinanceOrderBookEntry> GetOrderBookDepth(IEnumerable<BinanceOrderBookEntry> orders,
        decimal percentOfDepth, bool isAsks)
    {
        var ordersArray = orders.ToArray();
        
        var valueFromPercent = ordersArray.First().Price * (isAsks ? percentOfDepth : -percentOfDepth) / 100;
        var priceTo =  ordersArray.First().Price + valueFromPercent;

        return ordersArray.Where(x => isAsks ? x.Price <= priceTo : x.Price >= priceTo);
    }
    
    private static IEnumerable<TradedRange> GetTradedRanges(IReadOnlyCollection<BinanceClusterVolume> clusterVolumes, int step)
    {
        var maxPrice = clusterVolumes.Max(x => x.Price);
        var minPrice = clusterVolumes.Min(x => x.Price);
        var priceStep = (maxPrice - minPrice) / step;
        var tradedIndexes = new List<TradedRange>();

        var priceStart = maxPrice;

        for (var i = 0; i < step; i++)
        {
            var priceEnd = i == step - 1 ? minPrice : priceStart - priceStep;

            var clusters = i == step - 1 
                ? clusterVolumes.Where(x => x.Price <= priceStart && x.Price >= priceEnd).ToList() 
                : clusterVolumes.Where(x => x.Price <= priceStart && x.Price > priceEnd).ToList();

            var stepBinanceClusterVolume = new TradedRange
            {
                Index = i + 1
            };

            if (clusters.Any())
            {
                stepBinanceClusterVolume.StartPrice = clusters.Max(x => x.Price);
                stepBinanceClusterVolume.EndPrice = clusters.Min(x => x.Price);
                stepBinanceClusterVolume.SellVolume = clusters.Sum(x => x.SellVolume);
                stepBinanceClusterVolume.BuyVolume = clusters.Sum(x => x.BuyVolume);
                stepBinanceClusterVolume.BuyTrades = clusters.Sum(x => x.BuyTrades);
                stepBinanceClusterVolume.SellTrades = clusters.Sum(x => x.SellTrades);
            }
            else
            {
                stepBinanceClusterVolume.StartPrice = priceStart;
                stepBinanceClusterVolume.EndPrice = priceStart;
                stepBinanceClusterVolume.SellVolume = 0;
                stepBinanceClusterVolume.BuyVolume = 0;
                stepBinanceClusterVolume.BuyTrades = 0;
                stepBinanceClusterVolume.SellTrades = 0;
            }

            priceStart = priceEnd;

            tradedIndexes.Add(stepBinanceClusterVolume);
        }

        return tradedIndexes;
    }
    
    #endregion
}