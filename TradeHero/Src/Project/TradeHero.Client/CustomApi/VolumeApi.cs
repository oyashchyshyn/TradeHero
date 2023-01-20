using Binance.Net.Objects.Models.Spot;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Client.CustomApi;
using TradeHero.Core.Types.Client.Models;
using TradeHero.Core.Types.Client.Models.Response;
using TradeHero.Core.Types.Services;

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
        DateTime startFrom, DateTime endTo, CancellationToken cancellationToken = default)
    {
         if (startFrom > endTo)
         {
             return new ThWebCallResult<List<BinanceClusterVolume>>(new ThError(null, "'startFrom' cannot be grater then 'endTo'", null));
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

         var invalidTrades = collectionOfTrades.Where(x =>
             x.Price <= 0 || x.Quantity <= 0 || x.FirstTradeId <= -1 || x.LastTradeId <= -1);

         foreach (var invalidTrade in invalidTrades)
         {
             collectionOfTrades.Remove(invalidTrade);
         }

         var clusterVolumes = collectionOfTrades.GroupBy(x => x.Price)
             .Select(x =>
             {
                 var buys = x.Where(y => !y.BuyerIsMaker).ToList();
                 var sells = x.Where(y => y.BuyerIsMaker).ToList();

                 return new BinanceClusterVolume
                 {
                     Price = x.Key,
                     BuyVolume = buys.Any() ? buys.Sum(y => y.Quantity) : 0,
                     SellVolume = sells.Any() ? sells.Sum(y => y.Quantity) : 0,
                     BuyTrades = buys.Count,
                     SellTrades = sells.Count
                 };
             })
             .OrderByDescending(x => x.Price)
             .ToList();

         return new ThWebCallResult<List<BinanceClusterVolume>>(clusterVolumes);
    }
}