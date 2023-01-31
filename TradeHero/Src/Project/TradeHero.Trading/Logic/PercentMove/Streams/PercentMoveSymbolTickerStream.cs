using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Client;
using TradeHero.Trading.Base;
using TradeHero.Trading.Logic.PercentMove.Flow;

namespace TradeHero.Trading.Logic.PercentMove.Streams;

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

                var symbolInfo = _percentMoveStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.SingleOrDefault(x => x.Name == ticker.Symbol);
                if (symbolInfo == null)
                {
                    Logger.LogWarning("{Symbol}. {PropertyName} is null. In {Method}", ticker.Symbol, nameof(symbolInfo), nameof(ManageTickerAsync));

                    return;
                }
                
                var lastOrderPrice = _percentMoveStore.SymbolLastOrderPrice[ticker.Symbol];
                
                var isNeedToPlaceOrder = _percentMoveFilters.IsNeedToPlaceOrder(ticker.Symbol, ticker.LastPrice, 
                    lastOrderPrice, symbolInfo, _percentMoveStore.TradeLogicOptions);
                
                if (!isNeedToPlaceOrder)
                {
                    _percentMoveStore.SymbolStatus[ticker.Symbol] = true;
        
                    return;
                }

                var balance = _percentMoveStore.FuturesUsd.AccountData.Balances.SingleOrDefault(x => x.Asset == symbolInfo.QuoteAsset);
                if (balance == null)
                {
                    Logger.LogWarning("{Symbol}. {PropertyName} by quote ({QuoteName}) is null. In {Method}", 
                        ticker.Symbol, nameof(balance), symbolInfo.QuoteAsset, nameof(ManageTickerAsync));
                
                    return;
                }
                
                foreach (var openedPosition in _percentMoveStore.Positions.Where(x => x.Name == ticker.Symbol).ToArray())
                {
                    await _percentMoveEndpoints.CreateBuyMarketOrderAsync(openedPosition, symbolInfo, balance, cancellationToken: cancellationToken);
                }
        
                _percentMoveStore.SymbolStatus[ticker.Symbol] = true;
                    
            }, cancellationToken);

            return Task.CompletedTask;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Symbol}. {Message}. In {Method}",
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