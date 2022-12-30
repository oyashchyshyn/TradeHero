using Binance.Net.Objects.Models.Spot;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Client.CustomApi;
using TradeHero.Contracts.Client.Models;
using TradeHero.Contracts.Client.Models.Response;
using TradeHero.Contracts.Services;

namespace TradeHero.Client.CustomApi;

internal class VolumeApi : IVolumeApi
{
    private readonly IThRestBinanceClient _client;
    private readonly ICalculatorService _calculatorService;
    
    public VolumeApi(
        IThRestBinanceClient client, 
        ICalculatorService calculatorService
        )
    {
        _client = client;
        _calculatorService = calculatorService;
    }

    public async Task<ThWebCallResult<List<BinanceClusterVolume>>> GetClusterVolumeAsync(string symbol, Market market,
        DateTime startFrom, DateTime endTo, int step, CancellationToken cancellationToken = default)
    {
         if (startFrom > endTo)
         {
             return new ThWebCallResult<List<BinanceClusterVolume>>(new ThError(null, "'startFrom' cannot be grater then 'endTo'", null));
         }
        
         if (step < 0)
         {
             return new ThWebCallResult<List<BinanceClusterVolume>>(new ThError(null, "'step' cannot be less then zero", null));
         }

         var start = startFrom;
         var rangeInSeconds = (endTo - startFrom).TotalMilliseconds; // range between 2 dates
         var millisecondsList = _calculatorService.GetIterationValues(rangeInSeconds, ApiConstants.AggregatedTradeHistoryMaxDateRageInMilliseconds);
         var collectionOfTrades = new List<BinanceAggregatedTrade>();
         foreach (var end in millisecondsList.Select(milliseconds => start.AddMilliseconds(milliseconds)))
         {
             var collectionOfTradesInIteration = new List<BinanceAggregatedTrade>();
             var startDateTimeInIteration = start;
                
             do
             {
                 var getAggregatedTradeHistoryAsyncRequest = market switch
                 {
                     Market.Spot => await _client.SpotApi.ExchangeData.GetAggregatedTradeHistoryAsync(symbol, startTime: startDateTimeInIteration, 
                         endTime: end, limit: ApiConstants.LimitAggregatedTradeHistoryInRequest, ct: cancellationToken),
                     Market.Futures => await _client.UsdFuturesApi.ExchangeData.GetAggregatedTradeHistoryAsync(symbol, startTime: startDateTimeInIteration, 
                         endTime: end, limit: ApiConstants.LimitAggregatedTradeHistoryInRequest, ct: cancellationToken),
                     _ => throw new ArgumentOutOfRangeException(nameof(market), market, null)
                 };
                
                 if (!getAggregatedTradeHistoryAsyncRequest.Success)
                 {
                     return new ThWebCallResult<List<BinanceClusterVolume>>(getAggregatedTradeHistoryAsyncRequest.Error);
                 }

                 if (getAggregatedTradeHistoryAsyncRequest.Data.Any())
                 {
                     collectionOfTradesInIteration.AddRange(getAggregatedTradeHistoryAsyncRequest.Data);

                     if (getAggregatedTradeHistoryAsyncRequest.Data.Last().TradeTime < end
                         && getAggregatedTradeHistoryAsyncRequest.Data.Count() == ApiConstants.LimitAggregatedTradeHistoryInRequest)
                     {
                         startDateTimeInIteration = getAggregatedTradeHistoryAsyncRequest.Data.Last().TradeTime.AddMilliseconds(1);

                         continue;
                     }
                 }

                 break;
             } 
             while (true);
                
             collectionOfTrades.AddRange(collectionOfTradesInIteration);
             start = end.AddMilliseconds(1);
         }

         if (!collectionOfTrades.Any())
         {
             return new ThWebCallResult<List<BinanceClusterVolume>>(new List<BinanceClusterVolume>());
         }
            
         var clusterVolumes = collectionOfTrades.GroupBy(x => x.Price)
             .Select(x =>
             {
                 var buys = x.Where(y => !y.BuyerIsMaker).ToList();
                 var sells = x.Where(y => y.BuyerIsMaker).ToList();

                 return new BinanceClusterVolume
                 {
                     Index = 1,
                     StartPrice = x.Key,
                     EndPrice = x.Key,
                     BuyVolume = buys.Any() ? buys.Sum(y => y.Quantity) : 0,
                     SellVolume = sells.Any() ? sells.Sum(y => y.Quantity) : 0,
                     BuyOrders = buys.Count,
                     SellOrders = sells.Count
                 };
             })
             .OrderByDescending(x => x.StartPrice)
             .ToList();

         // Combine clusters in ranges
         if (step == 0 || !clusterVolumes.Any())
         {
             return new ThWebCallResult<List<BinanceClusterVolume>>(clusterVolumes);
         }

         var maxPrice = clusterVolumes.Max(x => x.StartPrice);
         var minPrice = clusterVolumes.Min(x => x.EndPrice);
         var priceStep = (maxPrice - minPrice) / step;
         var stepClusterVolumes = new List<BinanceClusterVolume>();

         var priceStart = maxPrice;
            
         for (var i = 0; i < step; i++)
         {
             List<BinanceClusterVolume> clusters;

             var priceEnd = i == step - 1 ? minPrice : priceStart - priceStep;
                
             if (i == step - 1)
             {
                 clusters = clusterVolumes.Where(x => x.StartPrice <= priceStart && x.EndPrice >= priceEnd)
                     .ToList();   
             }
             else
             {
                 clusters = clusterVolumes.Where(x => x.StartPrice <= priceStart && x.EndPrice > priceEnd)
                     .ToList();
             }

             var stepBinanceClusterVolume = new BinanceClusterVolume
             {
                 Index = i + 1
             };
                
             if (clusters.Any())
             {
                 stepBinanceClusterVolume.StartPrice = clusters.Max(x => x.StartPrice);
                 stepBinanceClusterVolume.EndPrice = clusters.Min(x => x.EndPrice);
                 stepBinanceClusterVolume.SellVolume = clusters.Sum(x => x.SellVolume);
                 stepBinanceClusterVolume.BuyVolume = clusters.Sum(x => x.BuyVolume);
                 stepBinanceClusterVolume.BuyOrders = clusters.Sum(x => x.BuyOrders);
                 stepBinanceClusterVolume.SellOrders = clusters.Sum(x => x.SellOrders);
             }
             else
             {
                 stepBinanceClusterVolume.StartPrice = priceStart;
                 stepBinanceClusterVolume.EndPrice = priceStart;
                 stepBinanceClusterVolume.SellVolume = 0;
                 stepBinanceClusterVolume.BuyVolume = 0;
                 stepBinanceClusterVolume.BuyOrders = 0;
                 stepBinanceClusterVolume.SellOrders = 0;
             }

             priceStart = priceEnd;
                
             stepClusterVolumes.Add(stepBinanceClusterVolume);
         }
        
         return new ThWebCallResult<List<BinanceClusterVolume>>(stepClusterVolumes);
    }
}