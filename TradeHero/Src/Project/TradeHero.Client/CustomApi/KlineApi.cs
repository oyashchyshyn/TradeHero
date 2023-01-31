using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TradeHero.Core.Constants;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Client;

namespace TradeHero.Client.CustomApi;

internal class KlineApi : IKlineApi
{
    private readonly IThRestBinanceClient _client;
    private readonly ICalculatorService _calculatorService;
    
    public KlineApi(
        IThRestBinanceClient client, 
        ICalculatorService calculatorService
        )
    {
        _client = client;
        _calculatorService = calculatorService;
    }
    
    public async Task<ThWebCallResult<List<IBinanceKline>>> GetKlineByDateRangeAsync(string symbolName, KlineInterval interval, 
        DateTime startFrom, DateTime endTo, Market market, CancellationToken cancellationToken = default)
    {
        try
        {
            if (startFrom > endTo)
            {
                return new ThWebCallResult<List<IBinanceKline>>(new ThError(null, "'startFrom' cannot be grater then 'endTo'", null));
            }
            
            var rangeInSeconds = (int)(endTo - startFrom).TotalSeconds;
            var totalCandles = rangeInSeconds / (int)interval;
            var startFromTime = startFrom;
            var listOfIterations = _calculatorService.GetIterationValues(totalCandles, ApiConstants.LimitKlineItemsInRequest);
            var klinesInfo = new List<IBinanceKline>();
            foreach (var iterationItem in listOfIterations)
            {
                var webCallResult = market switch
                {
                    Market.Spot => await _client.SpotApi.ExchangeData.GetKlinesAsync(symbolName, interval, startTime: startFromTime, endTime: 
                        endTo, iterationItem, cancellationToken),
                    Market.Futures => await _client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbolName, interval, startTime: startFromTime, 
                        endTime: endTo, limit: iterationItem, cancellationToken),
                    _ => throw new ArgumentOutOfRangeException(nameof(market), market, null)
                };

                if (webCallResult.Error != null)
                {
                    return new ThWebCallResult<List<IBinanceKline>>(webCallResult.Error);
                }
                
                startFromTime = webCallResult.Data.Last().CloseTime;

                klinesInfo.AddRange(webCallResult.Data);
            }

            return new ThWebCallResult<List<IBinanceKline>>(klinesInfo);
        }
        catch (Exception exception)
        {
            return new ThWebCallResult<List<IBinanceKline>>(new ThError(exception));
        }
    }
}