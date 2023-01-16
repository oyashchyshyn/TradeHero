using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Core.Enums;
using TradeHero.Trading.Base;
using TradeHero.Trading.Logic.PercentLimit.Enums;
using TradeHero.Trading.Logic.PercentLimit.Flow;

namespace TradeHero.Trading.Logic.PercentLimit.Streams;

internal class PercentLimitSymbolTickerStream : BaseFuturesUsdSymbolTickerStream
{
    private readonly PercentLimitStore _percentLimitStore;
    private readonly PercentLimitFilters _percentLimitFilters;
    private readonly PercentLimitEndpoints _percentLimitEndpoints;
    
    public PercentLimitSymbolTickerStream(
        ILogger<PercentLimitSymbolTickerStream> logger, 
        IThSocketBinanceClient socketBinanceClient, 
        PercentLimitStore percentLimitStore, 
        PercentLimitFilters percentLimitFilters, 
        PercentLimitEndpoints percentLimitEndpoints
        ) 
        : base(logger, socketBinanceClient)
    {
        _percentLimitStore = percentLimitStore;
        _percentLimitFilters = percentLimitFilters;
        _percentLimitEndpoints = percentLimitEndpoints;
    }

    protected override Task ManageTickerAsync(IBinance24HPrice ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_percentLimitStore.TradeLogicLogicOptions.EnableTrailingStops && !_percentLimitStore.TradeLogicLogicOptions.EnableMarketStopToExit)
            {
                return Task.CompletedTask;
            }
            
            var symbolInfo =
                _percentLimitStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == ticker.Symbol);
            
            var balance = _percentLimitStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == symbolInfo.QuoteAsset);
            
            foreach (var position in _percentLimitStore.Positions.Where(x => x.Name == ticker.Symbol))
            {
                Task.Run(async () =>
                {
                    var key = $"{position.Name}_{position.PositionSide}";
                    if (!_percentLimitStore.PositionsInfo.ContainsKey(key))
                    {
                        Logger.LogDebug("{Key} does not have status. In {Method}", 
                            key, nameof(ManageTickerAsync));
                    
                        return;
                    }

                    var positionInfo = _percentLimitStore.PositionsInfo[key];
                    
                    if (!positionInfo.IsNeedToCheckPosition)
                    {
                        Logger.LogDebug("{Key} with status {Status}. In {Method}", 
                            key, _percentLimitStore.PositionsInfo[key].IsNeedToCheckPosition, nameof(ManageTickerAsync));
                    
                        return;
                    }
                    
                    positionInfo.IsNeedToCheckPosition = false;

                    var orderToPlace = _percentLimitFilters.IsNeedToActivateOrders(
                        position, 
                        ticker.LastPrice,
                        _percentLimitStore.PositionsInfo[key],
                        balance,
                        _percentLimitStore.TradeLogicLogicOptions
                    );

                    switch (orderToPlace)
                    {
                        case PercentLimitOrderToPlace.MarketToClose:
                            var marketClosePositionResult = await _percentLimitEndpoints.CreateMarketClosePositionOrderAsync(
                                position,
                                symbolInfo,
                                cancellationToken: cancellationToken
                            );
                            if (marketClosePositionResult == ActionResult.Success)
                            {
                                return;
                            }   
                            break;
                        case PercentLimitOrderToPlace.MarketStopToSafe:
                            var stopLimitToSafeResult = await _percentLimitEndpoints.CreateMarketStopOrderAsync(
                                position,
                                ticker.LastPrice,
                                _percentLimitStore.TradeLogicLogicOptions.MarketStopSafePriceFromLastPricePercent,
                                symbolInfo,
                                cancellationToken: cancellationToken
                            );
                            if (stopLimitToSafeResult == ActionResult.Success)
                            {
                                positionInfo.IsNeedToPlaceMarketStop = false;
                            }   
                            break;
                        case PercentLimitOrderToPlace.MarketStopToClose:
                            var stopLimitToCloseResult = await _percentLimitEndpoints.CreateMarketStopOrderAsync(
                                position,
                                ticker.LastPrice,
                                _percentLimitStore.TradeLogicLogicOptions.MarketStopExitPriceFromLastPricePercent,
                                symbolInfo,
                                cancellationToken: cancellationToken
                            );
                            if (stopLimitToCloseResult == ActionResult.Success)
                            {
                                positionInfo.IsNeedToPlaceMarketStop = false;
                            }   
                            break;
                        case PercentLimitOrderToPlace.None:
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