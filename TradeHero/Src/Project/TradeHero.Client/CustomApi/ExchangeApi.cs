using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Client.CustomApi;
using TradeHero.Contracts.Client.Models;
using TradeHero.Contracts.Client.Models.Response;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;

namespace TradeHero.Client.CustomApi;

internal class ExchangeApi : IExchangeApi
{
    private readonly IThRestBinanceClient _client;
    private readonly ICalculatorService _calculatorService;

    public ExchangeApi(
        IThRestBinanceClient client, 
        ICalculatorService calculatorService
        )
    {
        _client = client;
        _calculatorService = calculatorService;
    }

    public async Task<ThWebCallResult<BinanceKlineVolatility>> GetVolatilityAsync(string symbolName, KlineInterval interval, int klinesBack, Market market, CancellationToken cancellationToken = default)
    {
        try
        {
            var seconds = (double)interval * (klinesBack + 1);
            var startFrom = DateTime.UtcNow.AddSeconds(-seconds);
            var endTo = DateTime.UtcNow;
            var startFromTime = startFrom;
            var listOfIterations = _calculatorService.GetIterationValues(klinesBack, ApiConstants.LimitKlineItemsInRequest);
            var klines = new List<IBinanceKline>();
            foreach (var iterationItem in listOfIterations)
            {
                var webCallResult = market switch
                {
                    Market.Spot => await _client.SpotApi.ExchangeData.GetKlinesAsync(symbolName, interval,
                        startTime: startFromTime, endTime: endTo, limit: iterationItem, cancellationToken),
                    Market.Futures => await _client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbolName, interval,
                        startTime: startFromTime, endTime: endTo, limit: iterationItem, cancellationToken),
                    _ => throw new ArgumentOutOfRangeException(nameof(market), market, null)
                };

                if (webCallResult.Error != null)
                {
                    return new ThWebCallResult<BinanceKlineVolatility>(webCallResult.Error);
                }

                startFromTime = webCallResult.Data.Last().CloseTime;
                klines.AddRange(webCallResult.Data);
            }

            var maxPrice = klines.Max(x => x.HighPrice);
            var lowPrice = klines.Min(x => x.LowPrice);
            
            var klineVolatility = new BinanceKlineVolatility
            {
                Interval = interval,
                KlinesCount = klinesBack,
                StartFrom = startFrom,
                EndTo = endTo,
                Volatility = _calculatorService.GetVolatility(maxPrice, lowPrice)
            };

            return new ThWebCallResult<BinanceKlineVolatility>(klineVolatility);
        }
        catch (Exception exception)
        {
            return new ThWebCallResult<BinanceKlineVolatility>(new ThError(null, exception.Message, null));
        }
    }
}