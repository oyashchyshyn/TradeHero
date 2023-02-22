using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Contracts.Trading;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Trading;
using TradeHero.Trading.Base;
using TradeHero.Trading.Endpoints.Rest;
using TradeHero.Trading.Helpers;
using TradeHero.Trading.Logic.PercentLimit.Flow;
using TradeHero.Trading.Logic.PercentLimit.Streams;

namespace TradeHero.Trading.Logic.PercentLimit;

internal class PercentLimitTradeLogic : BaseFuturesUsdTradeLogic
{
    private readonly PercentLimitPositionWorker _percentLimitPositionWorker;
    private readonly PercentLimitEndpoints _percentLimitEndpoints;
    private readonly PercentLimitFilters _percentLimitFilters;
    private readonly PercentLimitStore _percentLimitStore;

    public PercentLimitTradeLogic(
        ILogger<PercentLimitTradeLogic> logger,
        IThSocketBinanceClient binanceSocketClient,
        ITelegramService telegramService,
        IJobService jobService,
        IFuturesUsdEndpoints futuresUsdEndpoints,
        ISpotEndpoints spotEndpoints,
        IInstanceFactory instanceFactory,
        PercentLimitPositionWorker percentLimitPositionWorker, 
        PercentLimitEndpoints percentLimitEndpoints, 
        PercentLimitFilters percentLimitFilters,
        PercentLimitStore percentLimitStore, 
        PercentLimitUserAccountStream percentLimitUserAccountStream
        ) 
        : base(binanceSocketClient, jobService, spotEndpoints, instanceFactory,
            percentLimitUserAccountStream, logger, telegramService, futuresUsdEndpoints)
    {
        _percentLimitPositionWorker = percentLimitPositionWorker;
        _percentLimitEndpoints = percentLimitEndpoints;
        _percentLimitFilters = percentLimitFilters;
        _percentLimitStore = percentLimitStore;

        Store = _percentLimitStore;
    }

    protected override async Task RunInstanceAsync(BaseInstanceOptions instanceOptions, CancellationToken cancellationToken)
    {
        try
        {
            if (Instance == null)
            {
                return;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("CancellationToken is requested. In {Method}",
                    nameof(RunInstanceAsync));
                
                return;
            }

            var instanceResult = await Instance.GenerateInstanceResultAsync(
                _percentLimitStore, instanceOptions, cancellationToken
            );
            
            if (instanceResult.ActionResult != ActionResult.Success)
            {
                Logger.LogWarning("Result from is {ActionResult}. In {Method}", 
                    instanceResult.ActionResult, nameof(RunInstanceAsync));
                
                return;
            }

            if (_percentLimitStore.TradeLogicLogicOptions.EnableAveraging)
            {
                Logger.LogInformation("Averaging is enabled. In {Method}", 
                    nameof(RunInstanceAsync));
                
                await ManageAverageOrdersAsync(instanceResult.Data, cancellationToken);
            }

            if (_percentLimitStore.TradeLogicLogicOptions.EnableOpenPositions)
            {
                Logger.LogInformation("Open positions is enabled. In {Method}", 
                    nameof(RunInstanceAsync));
                
                await ManageMarketBuyOrdersAsync(instanceResult.Data, cancellationToken);
            }

            if (instanceOptions is { TelegramChannelId: { }, TelegramIsNeedToSendMessages: { } } 
                && instanceOptions.TelegramIsNeedToSendMessages.Value)
            {
                Logger.LogInformation("Preparing positions messaged for telegram. In {Method}", 
                    nameof(RunInstanceAsync));
                
                await SendMessageAsync(instanceResult.Data, 
                    instanceOptions.TelegramChannelId.Value, cancellationToken);   
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(RunInstanceAsync));
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(RunInstanceAsync));
        }
    }

    protected override async Task<ActionResult> CheckCurrentPositionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var changeLeverageResult = await FuturesUsdEndpoints.ChangeLeverageToAllPositionsAsync(
                _percentLimitStore,
                _percentLimitStore.TradeLogicLogicOptions.Leverage, 
                cancellationToken: CancellationTokenSource.Token
            );

            if (changeLeverageResult != ActionResult.Success)
            {
                return changeLeverageResult;
            }
            
            changeLeverageResult = await FuturesUsdEndpoints.ChangeMarginTypeToAllPositionsAsync(
                _percentLimitStore,
                _percentLimitStore.TradeLogicLogicOptions.MarginType, 
                cancellationToken: CancellationTokenSource.Token
            );
            
            if (changeLeverageResult != ActionResult.Success)
            {
                return changeLeverageResult;
            }
            
            var openedPositions = _percentLimitStore.FuturesUsd.AccountData.Positions
                .Where(x => x.EntryPrice != 0)
                .Where(x => x.Quantity != 0);

            foreach (var openedPosition in openedPositions)
            {
                var quantity = openedPosition.PositionSide == PositionSide.Short
                    ? Math.Abs(openedPosition.Quantity)
                    : openedPosition.Quantity;

                var createPositionResult = await _percentLimitPositionWorker.CreatePositionAsync(
                    _percentLimitStore,
                    openedPosition.Symbol,
                    openedPosition.PositionSide,
                    openedPosition.EntryPrice,
                    openedPosition.UpdateTime,
                    quantity,
                    true,
                    cancellationToken
                );

                if (createPositionResult != ActionResult.Success)
                {
                    return createPositionResult;
                }
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(CheckCurrentPositionsAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(CheckCurrentPositionsAsync));

            return ActionResult.SystemError;
        }
    }

    #region Private emthods

    private async Task ManageAverageOrdersAsync(InstanceResult instanceResult, CancellationToken cancellationToken)
    {
        foreach (var marketSignals in instanceResult.Signals)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("CancellationToken is requested. In {Method}",
                        nameof(ManageAverageOrdersAsync));
                
                    return;
                }

                if (_percentLimitStore.Positions.All(x => x.Name != marketSignals.FuturesUsdName))
                {
                    continue;
                }
                
                foreach (var openedPosition in _percentLimitStore.Positions.Where(x => x.Name == marketSignals.FuturesUsdName).ToArray())
                {
                    var symbolInfo = _percentLimitStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == openedPosition.Name);

                    var lastPriceRequest = await FuturesUsdEndpoints.GetSymbolLastPriceAsync(
                        openedPosition.Name, 
                        cancellationToken: CancellationTokenSource.Token
                    );

                    if (lastPriceRequest.ActionResult != ActionResult.Success)
                    {
                        continue;
                    }

                    var isAverageNeeded = await _percentLimitFilters.IsNeedToPlaceMarketAverageOrderAsync(instanceResult, openedPosition, lastPriceRequest.LastPrice, 
                        marketSignals, symbolInfo, _percentLimitStore.TradeLogicLogicOptions);
                    
                    if (!isAverageNeeded)
                    {
                        continue;
                    }
                
                    var balance = _percentLimitStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == openedPosition.QuoteAsset);

                    await _percentLimitEndpoints.CreateMarketAverageBuyOrderAsync(
                        openedPosition,
                        symbolInfo,
                        balance,
                        _percentLimitStore.TradeLogicLogicOptions,
                        cancellationToken: cancellationToken
                    );   
                }
            }
            catch (TaskCanceledException taskCanceledException)
            {
                Logger.LogWarning("{Message}. In {Method}",
                    taskCanceledException.Message, nameof(ManageAverageOrdersAsync));
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "In foreach of {Method}. Symbol {Symbol}", 
                    nameof(ManageAverageOrdersAsync), marketSignals.FuturesUsdName);
            }
        }
    }

    private async Task ManageMarketBuyOrdersAsync(InstanceResult instanceResult, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("CancellationToken is requested. In {Method}",
                    nameof(ManageMarketBuyOrdersAsync));
                
                return;
            }
            
            var filteredPositions = await _percentLimitFilters.GetFilteredOrdersForOpenPositionAsync(instanceResult, _percentLimitStore.TradeLogicLogicOptions, 
                _percentLimitStore.Positions, _percentLimitStore.FuturesUsd.AccountData.Positions.ToList());
            
            if (!filteredPositions.Any())
            {
                return;
            }
        
            foreach (var filteredPosition in filteredPositions)
            {
                var symbolInfo =
                    _percentLimitStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == filteredPosition.SymbolName);
                
                var positionInfo =
                    _percentLimitStore.FuturesUsd.AccountData.Positions.First(x => x.Symbol == filteredPosition.SymbolName);

                var balance = _percentLimitStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == filteredPosition.QuoteName);
                
                await _percentLimitEndpoints.CreateBuyMarketOrderAsync(
                    filteredPosition,
                    symbolInfo,
                    positionInfo,
                    balance,
                    _percentLimitStore.TradeLogicLogicOptions,
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(ManageMarketBuyOrdersAsync));
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(ManageMarketBuyOrdersAsync));
        }
    }

    private async Task SendMessageAsync(InstanceResult instanceResult, long channelId, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("CancellationToken is requested. In {Method}",
                    nameof(SendMessageAsync));
                
                return;
            }

            var statMessage = MessageGenerator.InstanceResultMessage(instanceResult);
            
            await TelegramService.SendTextMessageToChannelAsync(
                channelId, 
                statMessage, 
                cancellationToken: cancellationToken
            );
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SendMessageAsync));
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(SendMessageAsync));
        }
    }

    #endregion
}