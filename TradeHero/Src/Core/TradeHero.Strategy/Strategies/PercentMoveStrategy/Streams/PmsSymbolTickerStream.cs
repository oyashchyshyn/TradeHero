using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Flow;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy.Streams;

internal class PmsSymbolTickerStream : BaseFuturesUsdSymbolTickerStream
{
    private readonly PmsStore _pmsStore;
    private readonly PmsFilters _pmsFilters;
    private readonly PmsEndpoints _pmsEndpoints;
    
    public PmsSymbolTickerStream(
        ILogger<PmsSymbolTickerStream> logger, 
        IThSocketBinanceClient socketBinanceClient, 
        PmsStore pmsStore, 
        PmsFilters pmsFilters, 
        PmsEndpoints pmsEndpoints
        ) 
        : base(logger, socketBinanceClient)
    {
        _pmsStore = pmsStore;
        _pmsFilters = pmsFilters;
        _pmsEndpoints = pmsEndpoints;
    }

    protected override Task ManageTickerAsync(IBinance24HPrice ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            _pmsStore.MarketLastPrices[ticker.Symbol] = ticker.LastPrice;

            if (!_pmsStore.SymbolStatus.ContainsKey(ticker.Symbol) || !_pmsStore.SymbolStatus[ticker.Symbol] 
                || !_pmsStore.SymbolLastOrderPrice.ContainsKey(ticker.Symbol) || _pmsStore.SymbolLastOrderPrice[ticker.Symbol] == 0)
            {
                return Task.CompletedTask;
            }

            Task.Run(async () =>
            {
                _pmsStore.SymbolStatus[ticker.Symbol] = false;

                var symbolInfo =
                    _pmsStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == ticker.Symbol);
                
                var lastOrderPrice = _pmsStore.SymbolLastOrderPrice[ticker.Symbol];
                
                var isNeedToPlaceOrder = _pmsFilters.IsNeedToPlaceOrder(ticker.Symbol, ticker.LastPrice, 
                    lastOrderPrice, symbolInfo, _pmsStore.StrategyOptions);
                
                if (!isNeedToPlaceOrder)
                {
                    _pmsStore.SymbolStatus[ticker.Symbol] = true;
        
                    return;
                }

                var balance = _pmsStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == symbolInfo.QuoteAsset);
                
                foreach (var openedPosition in _pmsStore.Positions.Where(x => x.Name == ticker.Symbol))
                {
                    await _pmsEndpoints.CreateBuyMarketOrderAsync(openedPosition, symbolInfo, balance, cancellationToken: cancellationToken);
                }
        
                _pmsStore.SymbolStatus[ticker.Symbol] = true;
                    
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