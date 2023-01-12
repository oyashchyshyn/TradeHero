using System.Text;
using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Trading;
using TradeHero.Contracts.Trading.Models.Instance;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Trading.Base;
using TradeHero.Trading.Endpoints.Rest;
using TradeHero.Trading.Endpoints.Socket;
using TradeHero.Trading.Helpers;
using TradeHero.Trading.TradeLogic.PercentLimit.Flow;
using TradeHero.Trading.TradeLogic.PercentLimit.Streams;

namespace TradeHero.Trading.TradeLogic.PercentLimit;

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
        IFuturesUsdMarketTickerStream futuresUsdMarketTickerStream,
        PercentLimitPositionWorker percentLimitPositionWorker, 
        PercentLimitEndpoints percentLimitEndpoints, 
        PercentLimitFilters percentLimitFilters,
        PercentLimitStore percentLimitStore, 
        PercentLimitUserAccountStream percentLimitUserAccountStream
        ) 
        : base(binanceSocketClient, jobService, spotEndpoints, instanceFactory, futuresUsdMarketTickerStream,
            percentLimitUserAccountStream, percentLimitPositionWorker, logger, telegramService, futuresUsdEndpoints)
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
                Logger.LogWarning("CancellationToken is requested. In {Method}",
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

            Logger.LogInformation("Results. Longs: {LongsCount}. Shorts: {ShortsCount}. In {Method}", 
                instanceResult.Data.LongSignals.Count, instanceResult.Data.ShortSignals.Count, 
                nameof(RunInstanceAsync));

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
            Logger.LogWarning("{Message}. In {Method}",
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
        foreach (var marketSignals in instanceResult.ShortSignals.Concat(instanceResult.LongSignals))
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogWarning("CancellationToken is requested. In {Method}",
                        nameof(ManageAverageOrdersAsync));
                
                    return;
                }
                
                var openedPosition = _percentLimitStore.Positions
                    .Where(x => x.Name == marketSignals.FuturesUsdName)
                    .SingleOrDefault(x => x.PositionSide == marketSignals.KlinePositionSignal);

                if (openedPosition == null)
                {
                    continue;
                }

                var symbolInfo =
                    _percentLimitStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == openedPosition.Name);

                var lastPrice = _percentLimitStore.MarketLastPrices[openedPosition.Name];
                
                var isAverageNeeded = await _percentLimitFilters.IsNeedToPlaceMarketAverageOrderAsync(instanceResult, openedPosition, lastPrice, 
                    marketSignals, symbolInfo, _percentLimitStore.TradeLogicLogicOptions);
                if (!isAverageNeeded)
                {
                    continue;
                }
                
                var balance = 
                    _percentLimitStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == openedPosition.QuoteAsset);

                await _percentLimitEndpoints.CreateMarketAverageBuyOrderAsync(
                    openedPosition, 
                    lastPrice,
                    symbolInfo,
                    balance,
                    _percentLimitStore.TradeLogicLogicOptions,
                    cancellationToken: cancellationToken
                );
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
                Logger.LogWarning("CancellationToken is requested. In {Method}",
                    nameof(ManageMarketBuyOrdersAsync));
                
                return;
            }
            
            //TODO: Swap this variable with if after testing
            var filteredPositions = await _percentLimitFilters.GetFilteredOrdersForOpenPositionAsync(instanceResult, _percentLimitStore.TradeLogicLogicOptions, 
                _percentLimitStore.Positions, _percentLimitStore.FuturesUsd.AccountData.Positions.ToList());
            
            if (_percentLimitStore.Positions.Count >= _percentLimitStore.TradeLogicLogicOptions.MaximumPositions)
            {
                Logger.LogWarning("Cannot open new positions. Opened positions: {OpenedPositions}, available: {AvailablePositions}. In {Method}",
                    _percentLimitStore.Positions.Count, _percentLimitStore.TradeLogicLogicOptions.MaximumPositions, nameof(ManageMarketBuyOrdersAsync));
                
                return;
            }
        
            foreach (var filteredPosition in filteredPositions)
            {
                var positionInfo =
                    _percentLimitStore.FuturesUsd.AccountData.Positions.First(x => x.Symbol == filteredPosition.FuturesUsdName);

                var lastPrice = _percentLimitStore.MarketLastPrices[filteredPosition.FuturesUsdName];
                
                var symbolInfo =
                    _percentLimitStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == filteredPosition.FuturesUsdName);
                
                var balance = _percentLimitStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == filteredPosition.QuoteAsset);
                
                await _percentLimitEndpoints.CreateBuyMarketOrderAsync(
                    filteredPosition, 
                    lastPrice,
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
                Logger.LogWarning("CancellationToken is requested. In {Method}",
                    nameof(SendMessageAsync));
                
                return;
            }

            var statMessage = MessageGenerator.InstanceResultMessage(instanceResult);
            
            await TelegramService.SendTextMessageToChannelAsync(
                channelId, 
                statMessage, 
                cancellationToken: cancellationToken
            );

            if (instanceResult.ShortSignals.Any())
            {
                var shortMessages = new List<StringBuilder> { new($"SHORTS{Environment.NewLine}{Environment.NewLine}") };
                var shortIndex = 0;

                foreach (var symbolsInfoContainer in instanceResult.ShortSignals)
                {
                    var message = MessageGenerator.PositionMessage(symbolsInfoContainer);

                    if (shortMessages[shortIndex].Length + message.Length <= TelegramConstants.MaximumMessageLenght)
                    {
                        shortMessages[shortIndex].Append(message);

                        continue;
                    }

                    shortMessages.Add(new StringBuilder(message));
                    shortIndex += 1;
                }

                foreach (var shortPositionsMessage in shortMessages)
                {
                    await TelegramService.SendTextMessageToChannelAsync(
                        channelId, 
                        shortPositionsMessage.ToString(), 
                        cancellationToken: cancellationToken
                    );
                }
            }
        
            if (instanceResult.LongSignals.Any())
            {
                var longMessages = new List<StringBuilder> { new($"LONGS{Environment.NewLine}{Environment.NewLine}") };
                var longIndex = 0;
            
                foreach (var symbolsInfoContainer in instanceResult.LongSignals)
                {
                    var message = MessageGenerator.PositionMessage(symbolsInfoContainer);
                
                    if (longMessages[longIndex].Length + message.Length <= TelegramConstants.MaximumMessageLenght)
                    {
                        longMessages[longIndex].Append(message);
                    
                        continue;
                    }
                
                    longMessages.Add(new StringBuilder(message));
                    longIndex += 1;
                }
            
                foreach (var longPositionsMessage in longMessages)
                {
                    await TelegramService.SendTextMessageToChannelAsync(
                        channelId, 
                        longPositionsMessage.ToString(), 
                        cancellationToken: cancellationToken
                    );
                }
            }
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