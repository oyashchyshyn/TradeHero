using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Exceptions;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.StrategyRunner;

namespace TradeHero.StrategyRunner.Endpoints.Rest.Implementation;

internal class FuturesUsdEndpoints : IFuturesUsdEndpoints
{
    private readonly ILogger<FuturesUsdEndpoints> _logger;
    private readonly IThRestBinanceClient _restBinanceClient;

    public FuturesUsdEndpoints(
        ILogger<FuturesUsdEndpoints> logger, 
        IThRestBinanceClient restBinanceClient
        )
    {
        _logger = logger;
        _restBinanceClient = restBinanceClient;
    }

    public async Task<ActionResult> SetFuturesUsdWalletBalancesAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(SetFuturesUsdWalletBalancesAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var walletBalancesRequest = await _restBinanceClient.UsdFuturesApi.Account.GetBalancesAsync(
                    ct:cancellationToken
                );
            
                if (walletBalancesRequest.Success)
                {
                    store.FuturesUsd.AccountData.Balances = walletBalancesRequest.Data;

                    break;
                }

                _logger.LogWarning(new ThException(walletBalancesRequest.Error),"In {Method}",
                    nameof(SetFuturesUsdWalletBalancesAsync));
                
                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(SetFuturesUsdWalletBalancesAsync));

                return ActionResult.ClientError;
            }

            _logger.LogInformation("Set binance futures-usd wallet balance data. In {Method}", 
                nameof(SetFuturesUsdWalletBalancesAsync));

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SetFuturesUsdWalletBalancesAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetFuturesUsdWalletBalancesAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> SetFuturesUsdExchangeInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(SetFuturesUsdExchangeInfoAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var futureUsdtSymbolInfoRequest = await _restBinanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(
                    ct: cancellationToken
                );
            
                if (futureUsdtSymbolInfoRequest.Success)
                {
                    store.FuturesUsd.ExchangerData.ExchangeInfo = futureUsdtSymbolInfoRequest.Data;
                    
                    break;
                }
                
                _logger.LogWarning(new ThException(futureUsdtSymbolInfoRequest.Error),"In {Method}",
                    nameof(SetFuturesUsdExchangeInfoAsync));
                
                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(SetFuturesUsdExchangeInfoAsync));
                
                return ActionResult.ClientError;
            }

            _logger.LogInformation("Set binance futures-usd exchange info. Symbols: {SymbolsCount}. Assets: {AssetsCount}. In {Method}", 
                store.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Count(), 
                store.FuturesUsd.ExchangerData.ExchangeInfo.Assets.Count(),
                nameof(SetFuturesUsdExchangeInfoAsync)
            );

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SetFuturesUsdExchangeInfoAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetFuturesUsdExchangeInfoAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> SetFuturesUsdPositionInfoAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(SetFuturesUsdPositionInfoAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var futuresUsdPositionInfoRequest = await _restBinanceClient.UsdFuturesApi.Account.GetPositionInformationAsync(
                    ct: cancellationToken
                );
            
                if (futuresUsdPositionInfoRequest.Success)
                {
                    store.FuturesUsd.AccountData.Positions = futuresUsdPositionInfoRequest.Data;

                    break;
                }

                _logger.LogWarning(new ThException(futuresUsdPositionInfoRequest.Error),"In {Method}",
                    nameof(SetFuturesUsdPositionInfoAsync));
                
                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(SetFuturesUsdPositionInfoAsync));
                
                return ActionResult.ClientError;
            }

            _logger.LogInformation("Set futures-usd positions. Positions: {Value}. In {Method}", 
                store.FuturesUsd.AccountData.Positions.Count(), nameof(SetFuturesUsdPositionInfoAsync));

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SetFuturesUsdPositionInfoAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetFuturesUsdPositionInfoAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> SetFuturesUsdStreamListenKeyAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(SetFuturesUsdStreamListenKeyAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var streamListenKeyRequest = await _restBinanceClient.UsdFuturesApi.Account.StartUserStreamAsync(
                    ct: cancellationToken
                );
            
                if (streamListenKeyRequest.Success)
                {
                    store.FuturesUsd.ExchangerData.StreamListenKey = streamListenKeyRequest.Data;
                    
                    break;
                }

                _logger.LogWarning(new ThException(streamListenKeyRequest.Error),"In {Method}",
                    nameof(SetFuturesUsdStreamListenKeyAsync));
                
                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(SetFuturesUsdStreamListenKeyAsync));
                
                return ActionResult.ClientError;
            }

            _logger.LogInformation("Set futures-usd stream listen key. Listen key: {Value}. In {Method}", 
                store.FuturesUsd.ExchangerData.StreamListenKey, nameof(SetFuturesUsdStreamListenKeyAsync));

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SetFuturesUsdStreamListenKeyAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetFuturesUsdStreamListenKeyAsync));

            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> ChangeMarginTypeToAllPositionsAsync(ITradeLogicStore store, FuturesMarginType marginType, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var position in store.FuturesUsd.AccountData.Positions.DistinctBy(x => x.Symbol))
            {
                if (position.MarginType == marginType)
                {
                    continue;
                }

                const int localRetries = 2;
                for (var i = 0; i < localRetries; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Cancellation token is requested. In {Method}", 
                            nameof(ChangeMarginTypeToAllPositionsAsync));

                        return ActionResult.CancellationTokenRequested;
                    }
                    
                    var changeMarginTypeRequest = await _restBinanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(
                        position.Symbol,
                        marginType,
                        ct: cancellationToken
                    );
                
                    if (changeMarginTypeRequest.Success)
                    {
                                                
                        _logger.LogInformation("{Symbol}. Margin type changed successfully to {MarginType}. In {Method}", 
                            position.Symbol, marginType, nameof(ChangeMarginTypeToAllPositionsAsync));
                        
                        break;
                    }

                    _logger.LogWarning(new ThException(changeMarginTypeRequest.Error),"{Symbol}. In {Method}",
                        position.Symbol, nameof(ChangeMarginTypeToAllPositionsAsync));
                    
                    if (i != localRetries - 1)
                    {
                        continue;
                    }
                    
                    _logger.LogWarning("{Symbol}. {Number} retries exceeded In {Method}",
                        position.Symbol, localRetries, nameof(ChangeMarginTypeToAllPositionsAsync));
                }
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(ChangeMarginTypeToAllPositionsAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ChangeMarginTypeToAllPositionsAsync));

            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> ChangeLeverageToAllPositionsAsync(ITradeLogicStore store, int leverage, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var binanceFuturesSymbolBrackets = new List<BinanceFuturesSymbolBracket>();
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancellation token is requested. In {Method}", 
                        nameof(ChangeLeverageToAllPositionsAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var leveragesInfoRequest = await _restBinanceClient.UsdFuturesApi.Account.GetBracketsAsync(
                    ct: cancellationToken
                );

                if (leveragesInfoRequest.Success)
                {
                    _logger.LogInformation("Brackets count {BracketsCount}. In {Method}",
                        leveragesInfoRequest.Data.Count(), nameof(ChangeLeverageToAllPositionsAsync));
                    
                    binanceFuturesSymbolBrackets.AddRange(leveragesInfoRequest.Data);

                    break;
                }

                _logger.LogWarning(new ThException(leveragesInfoRequest.Error),"In {Method}",
                    nameof(ChangeLeverageToAllPositionsAsync));

                if (i == maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded. In {Method}",
                    maxRetries, nameof(ChangeLeverageToAllPositionsAsync));

                return ActionResult.ClientError;
            }

            foreach (var position in store.FuturesUsd.AccountData.Positions.DistinctBy(x => x.Symbol))
            {
                if (position.Leverage == leverage)
                {
                    continue;
                }
                
                var leverageInfoForSymbol = binanceFuturesSymbolBrackets.Single(x => x.Symbol == position.Symbol);
                var maxLeverage = leverageInfoForSymbol.Brackets.MaxBy(x => x.InitialLeverage)?.InitialLeverage ?? -1;
                    
                if (maxLeverage == -1 || maxLeverage < leverage)
                {
                    _logger.LogInformation("{Symbol}. Cannot change leverage, because maximum leverage for position is {Leverage}. In {Method}", 
                        position.Symbol, maxLeverage, nameof(ChangeLeverageToAllPositionsAsync));
                        
                    continue;
                }

                const int localRetries = 2;
                for (var i = 0; i < localRetries; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Cancellation token is requested. In {Method}", 
                            nameof(ChangeLeverageToAllPositionsAsync));

                        return ActionResult.CancellationTokenRequested;
                    }
                    
                    var changeLeverageRequest = await _restBinanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(
                        position.Symbol,
                        leverage,
                        ct: cancellationToken
                    );
                
                    if (changeLeverageRequest.Success)
                    {
                        _logger.LogInformation("{Symbol}. Leverage changed successfully to x{Leverage}. In {Method}", 
                            position.Symbol, leverage, nameof(ChangeLeverageToAllPositionsAsync));

                        break;
                    }

                    _logger.LogWarning(new ThException(changeLeverageRequest.Error),"In {Method}",
                        nameof(ChangeLeverageToAllPositionsAsync));
                    
                    if (i != localRetries - 1)
                    {
                        continue;
                    }
                    
                    _logger.LogWarning("{Symbol}. {Number} retries exceeded In {Method}",
                        position.Symbol, localRetries, nameof(ChangeLeverageToAllPositionsAsync));
                }
            }
        
            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(ChangeLeverageToAllPositionsAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ChangeLeverageToAllPositionsAsync));

            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> ChangeSymbolLeverageToAvailableAsync(string symbol, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancellation token is requested. In {Method}", 
                        nameof(ChangeLeverageToAllPositionsAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var leveragesInfoRequest = await _restBinanceClient.UsdFuturesApi.Account.GetBracketsAsync(
                    symbol, 
                    ct: cancellationToken
                );
            
                if (!leveragesInfoRequest.Success)
                {
                    _logger.LogWarning(new ThException(leveragesInfoRequest.Error),"In {Method}",
                        nameof(ChangeSymbolLeverageToAvailableAsync));
                    
                    continue;
                }

                var availableLeverageForSymbol = leveragesInfoRequest.Data.Single(x => x.Symbol == symbol)
                    .Brackets.First().InitialLeverage;
            
                var changeLeverageRequest = await _restBinanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(
                    symbol, 
                    availableLeverageForSymbol, 
                    ct: cancellationToken
                );
        
                _logger.LogWarning(new ThException(changeLeverageRequest.Error),"In {Method}",
                    nameof(ChangeSymbolLeverageToAvailableAsync));

                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(DestroyStreamListerKeyAsync));

                return ActionResult.ClientError;
            }

            _logger.LogInformation("{Symbol}. Leverage changed successfully. In {Method}",
                symbol, nameof(ChangeSymbolLeverageToAvailableAsync));

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(ChangeSymbolLeverageToAvailableAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "{Symbol}. Cannot manage leverage change request. In {Method}",
                symbol, nameof(ChangeSymbolLeverageToAvailableAsync));

            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> CancelOpenedOrdersAsync(string symbol, PositionSide side, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var listBinanceFuturesOrders = new List<BinanceFuturesOrder>();
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancellation token is requested. In {Method}", 
                        nameof(CancelOpenedOrdersAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var symbolOpenedOrdersRequest = await _restBinanceClient.UsdFuturesApi.Trading.GetOpenOrdersAsync(
                    symbol,
                    ct: cancellationToken
                );
            
                if (symbolOpenedOrdersRequest.Success)
                {
                    _logger.LogInformation("{Symbol} | {Side}. Orders count {BracketsCount}. In {Method}",
                        symbol, side, symbolOpenedOrdersRequest.Data.Count(), nameof(CancelOpenedOrdersAsync));
                    
                    if (symbolOpenedOrdersRequest.Data.Any())
                    {
                        listBinanceFuturesOrders.AddRange(symbolOpenedOrdersRequest.Data);   
                    }

                    break;
                }

                _logger.LogWarning(new ThException(symbolOpenedOrdersRequest.Error),"{Symbol} | {Side}. In {Method}",
                    symbol, side, nameof(CancelOpenedOrdersAsync));
                
                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(UpdateStreamListerKeyAsync));

                return ActionResult.ClientError;
            }

            if (!listBinanceFuturesOrders.Any())
            {
                _logger.LogInformation("{Symbol} | {Side}. There is no orders to delete. In {Method}",
                    symbol, side, nameof(CancelOpenedOrdersAsync));

                return ActionResult.Success;
            }
            
            var openedOrders = listBinanceFuturesOrders
                .Where(x => x.Status == OrderStatus.New)
                .Where(x => x.PositionSide == side);
                
            foreach (var openedOrder in openedOrders)
            {
                const int localMaxRetries = 2;
                for (var i = 0; i < localMaxRetries; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Cancellation token is requested. In {Method}", 
                            nameof(CancelOpenedOrdersAsync));

                        return ActionResult.CancellationTokenRequested;
                    }
                    
                    var cancelOpenedOrderRequest = await _restBinanceClient.UsdFuturesApi.Trading.CancelOrderAsync(
                        symbol,
                        openedOrder.Id,
                        ct: cancellationToken
                    );

                    if (cancelOpenedOrderRequest.Success)
                    {
                        _logger.LogInformation("{Symbol} | {Side}. Opened order with id {Id} removed successfully. In {Method}",
                            symbol, side, openedOrder.Id, nameof(CancelOpenedOrdersAsync)); 
                        
                        break;
                    }

                    _logger.LogWarning(new ThException(cancelOpenedOrderRequest.Error),"{Symbol} | {Side} | OrderId {Id}. In {Method}",
                        symbol, side, openedOrder.Id, nameof(CancelOpenedOrdersAsync));

                    if (cancelOpenedOrderRequest.Error?.Code == (int)ApiErrorCodes.UnknownOrderWasSent)
                    {
                        _logger.LogInformation("{Symbol} | {Side}. Opened order with id {Id} does not exist. In {Method}",
                            symbol, side, openedOrder.Id, nameof(CancelOpenedOrdersAsync)); 
                        
                        break;
                    }
                    
                    if (i != localMaxRetries - 1)
                    {
                        continue;
                    }
                
                    _logger.LogError("{Symbol} | {Side} | OrderId {Id}. {Number} retries exceeded In {Method}",
                        symbol, side, openedOrder.Id, localMaxRetries, nameof(UpdateStreamListerKeyAsync));
                }
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CancelOpenedOrdersAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(CancelOpenedOrdersAsync));

            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> UpdateStreamListerKeyAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(UpdateStreamListerKeyAsync));

                    return ActionResult.CancellationTokenRequested;
                }
                
                var keepAliveUserStream = await _restBinanceClient.UsdFuturesApi.Account.KeepAliveUserStreamAsync(
                    store.FuturesUsd.ExchangerData.StreamListenKey,
                    ct: cancellationToken
                );
            
                if (keepAliveUserStream.Success)
                {
                    break;
                }

                _logger.LogWarning(new ThException(keepAliveUserStream.Error),"In {Method}",
                    nameof(UpdateStreamListerKeyAsync));

                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(UpdateStreamListerKeyAsync));

                return ActionResult.ClientError;
            }

            _logger.LogInformation("Stream listen key is updated. In {Method}", 
                nameof(UpdateStreamListerKeyAsync));

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(UpdateStreamListerKeyAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(UpdateStreamListerKeyAsync));

            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> DestroyStreamListerKeyAsync(ITradeLogicStore store, int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(DestroyStreamListerKeyAsync));

                    return ActionResult.CancellationTokenRequested;
                }
            
                var removeFuturesUsdUserStreamListenKeyRequest = await _restBinanceClient.UsdFuturesApi.Account.StopUserStreamAsync(
                    store.FuturesUsd.ExchangerData.StreamListenKey,
                    ct: cancellationToken
                );
            
                if (removeFuturesUsdUserStreamListenKeyRequest.Success)
                {
                    break;
                }
                
                _logger.LogError(new ThException(removeFuturesUsdUserStreamListenKeyRequest.Error), 
                    "In {Method}", nameof(DestroyStreamListerKeyAsync));

                if (i != maxRetries - 1)
                {
                    continue;
                }
                
                _logger.LogError("{Number} retries exceeded In {Method}",
                    maxRetries, nameof(DestroyStreamListerKeyAsync));

                return ActionResult.ClientError;
            }

            _logger.LogInformation("Futures-usd listen key is destroyed");

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(DestroyStreamListerKeyAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(DestroyStreamListerKeyAsync));

            return ActionResult.SystemError;
        }
    }
}