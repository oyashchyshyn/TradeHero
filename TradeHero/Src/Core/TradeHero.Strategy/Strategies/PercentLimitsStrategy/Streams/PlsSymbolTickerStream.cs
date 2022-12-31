using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Client;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Enums;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Flow;

namespace TradeHero.Strategies.Strategies.PercentLimitsStrategy.Streams;

internal class PlsSymbolTickerStream : BaseFuturesUsdSymbolTickerStream
{
    private readonly PlsStore _plsStore;
    private readonly PlsFilters _plsFilters;
    private readonly PlsEndpoints _plsEndpoints;
    
    public PlsSymbolTickerStream(
        ILogger<PlsSymbolTickerStream> logger, 
        IThSocketBinanceClient socketBinanceClient, 
        PlsStore plsStore, 
        PlsFilters plsFilters, 
        PlsEndpoints plsEndpoints
        ) 
        : base(logger, socketBinanceClient)
    {
        _plsStore = plsStore;
        _plsFilters = plsFilters;
        _plsEndpoints = plsEndpoints;
    }

    protected override Task ManageTickerAsync(IBinance24HPrice ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_plsStore.TradeLogicOptions.EnableTrailingStops && !_plsStore.TradeLogicOptions.EnableMarketStopToExit)
            {
                return Task.CompletedTask;
            }
            
            var symbolInfo =
                _plsStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == ticker.Symbol);
            
            var balance = _plsStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == symbolInfo.QuoteAsset);
            
            foreach (var position in _plsStore.Positions.Where(x => x.Name == ticker.Symbol))
            {
                Task.Run(async () =>
                {
                    var key = $"{position.Name}_{position.PositionSide}";
                    if (!_plsStore.PositionsInfo.ContainsKey(key))
                    {
                        Logger.LogDebug("{Key} does not have status. In {Method}", 
                            key, nameof(ManageTickerAsync));
                    
                        return;
                    }

                    var positionInfo = _plsStore.PositionsInfo[key];
                    
                    if (!positionInfo.IsNeedToCheckPosition)
                    {
                        Logger.LogDebug("{Key} with status {Status}. In {Method}", 
                            key, _plsStore.PositionsInfo[key].IsNeedToCheckPosition, nameof(ManageTickerAsync));
                    
                        return;
                    }
                    
                    positionInfo.IsNeedToCheckPosition = false;

                    var orderToPlace = _plsFilters.IsNeedToActivateOrders(
                        position, 
                        ticker.LastPrice,
                        _plsStore.PositionsInfo[key],
                        balance,
                        _plsStore.TradeLogicOptions
                    );

                    switch (orderToPlace)
                    {
                        case OrderToPlace.MarketToClose:
                            var marketClosePositionResult = await _plsEndpoints.CreateMarketClosePositionOrderAsync(
                                position,
                                symbolInfo,
                                cancellationToken: cancellationToken
                            );
                            if (marketClosePositionResult == ActionResult.Success)
                            {
                                return;
                            }   
                            break;
                        case OrderToPlace.MarketStopToSafe:
                            var stopLimitToSafeResult = await _plsEndpoints.CreateMarketStopOrderAsync(
                                position,
                                ticker.LastPrice,
                                _plsStore.TradeLogicOptions.MarketStopSafePriceFromLastPricePercent,
                                symbolInfo,
                                cancellationToken: cancellationToken
                            );
                            if (stopLimitToSafeResult == ActionResult.Success)
                            {
                                positionInfo.IsNeedToPlaceMarketStop = false;
                            }   
                            break;
                        case OrderToPlace.MarketStopToClose:
                            var stopLimitToCloseResult = await _plsEndpoints.CreateMarketStopOrderAsync(
                                position,
                                ticker.LastPrice,
                                _plsStore.TradeLogicOptions.MarketStopExitPriceFromLastPricePercent,
                                symbolInfo,
                                cancellationToken: cancellationToken
                            );
                            if (stopLimitToCloseResult == ActionResult.Success)
                            {
                                positionInfo.IsNeedToPlaceMarketStop = false;
                            }   
                            break;
                        case OrderToPlace.None:
                        default:
                            break;
                    }

                    positionInfo.IsNeedToCheckPosition = true;
                    
                }, cancellationToken);
            }

            return Task.CompletedTask;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(ManageTickerAsync));

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "{Symbol}. In {Method}", 
                ticker.Symbol, nameof(ManageTickerAsync));
            
            return Task.CompletedTask;
        }
    }
}