using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.TradeLogic.PercentMove.Flow;

namespace TradeHero.Strategies.TradeLogic.PercentMove.Streams;

internal class PercentMoveSymbolTickerStream : BaseFuturesUsdSymbolTickerStream
{
    private readonly PercentMoveStore _percentMoveStore;
    private readonly PercentMoveFilters _percentMoveFilters;
    private readonly PercentMoveEndpoints _percentMoveEndpoints;
    
    public PercentMoveSymbolTickerStream(
        ILogger<PercentMoveSymbolTickerStream> logger, 
        IThSocketBinanceClient socketBinanceClient, 
        PercentMoveStore percentMoveStore, 
        PercentMoveFilters percentMoveFilters, 
        PercentMoveEndpoints percentMoveEndpoints
        ) 
        : base(logger, socketBinanceClient)
    {
        _percentMoveStore = percentMoveStore;
        _percentMoveFilters = percentMoveFilters;
        _percentMoveEndpoints = percentMoveEndpoints;
    }

    protected override Task ManageTickerAsync(IBinance24HPrice ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            _percentMoveStore.MarketLastPrices[ticker.Symbol] = ticker.LastPrice;

            if (!_percentMoveStore.SymbolStatus.ContainsKey(ticker.Symbol) || !_percentMoveStore.SymbolStatus[ticker.Symbol] 
                || !_percentMoveStore.SymbolLastOrderPrice.ContainsKey(ticker.Symbol) || _percentMoveStore.SymbolLastOrderPrice[ticker.Symbol] == 0)
            {
                return Task.CompletedTask;
            }

            Task.Run(async () =>
            {
                _percentMoveStore.SymbolStatus[ticker.Symbol] = false;

                var symbolInfo =
                    _percentMoveStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == ticker.Symbol);
                
                var lastOrderPrice = _percentMoveStore.SymbolLastOrderPrice[ticker.Symbol];
                
                var isNeedToPlaceOrder = _percentMoveFilters.IsNeedToPlaceOrder(ticker.Symbol, ticker.LastPrice, 
                    lastOrderPrice, symbolInfo, _percentMoveStore.TradeLogicOptions);
                
                if (!isNeedToPlaceOrder)
                {
                    _percentMoveStore.SymbolStatus[ticker.Symbol] = true;
        
                    return;
                }

                var balance = _percentMoveStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == symbolInfo.QuoteAsset);
                
                foreach (var openedPosition in _percentMoveStore.Positions.Where(x => x.Name == ticker.Symbol))
                {
                    await _percentMoveEndpoints.CreateBuyMarketOrderAsync(openedPosition, symbolInfo, balance, cancellationToken: cancellationToken);
                }
        
                _percentMoveStore.SymbolStatus[ticker.Symbol] = true;
                    
            }, cancellationToken);

            return Task.CompletedTask;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogWarning("{Symbol}. {Message}. In {Method}",
                ticker.Symbol, taskCanceledException.Message, nameof(ManageTickerAsync));

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