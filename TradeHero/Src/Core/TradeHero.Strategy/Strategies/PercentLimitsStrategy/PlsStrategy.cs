using System.Text;
using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Client;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Strategy;
using TradeHero.Contracts.Strategy.Models.Instance;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.Endpoints.Rest;
using TradeHero.Strategies.Endpoints.Socket;
using TradeHero.Strategies.Helpers;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Flow;
using TradeHero.Strategies.Strategies.PercentLimitsStrategy.Streams;

namespace TradeHero.Strategies.Strategies.PercentLimitsStrategy;

internal class PlsStrategy : BaseFuturesUsdStrategy
{
    private readonly PlsPositionWorker _plsPositionWorker;
    private readonly PlsEndpoints _plsEndpoints;
    private readonly PlsFilters _plsFilters;
    private readonly PlsStore _plsStore;

    public PlsStrategy(
        ILogger<PlsStrategy> logger,
        IThSocketBinanceClient binanceSocketClient,
        ITelegramService telegramService,
        IJobService jobService,
        IFuturesUsdEndpoints futuresUsdEndpoints,
        ISpotEndpoints spotEndpoints,
        IInstanceFactory instanceFactory,
        IFuturesUsdMarketTickerStream futuresUsdMarketTickerStream,
        PlsPositionWorker plsPositionWorker, 
        PlsEndpoints plsEndpoints, 
        PlsFilters plsFilters,
        PlsStore plsStore, 
        PlsUserAccountStream plsUserAccountStream
        ) 
        : base(binanceSocketClient, jobService, spotEndpoints, instanceFactory, futuresUsdMarketTickerStream,
            plsUserAccountStream, plsPositionWorker, logger, telegramService, futuresUsdEndpoints)
    {
        _plsPositionWorker = plsPositionWorker;
        _plsEndpoints = plsEndpoints;
        _plsFilters = plsFilters;
        _plsStore = plsStore;

        Store = _plsStore;
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
                _plsStore, instanceOptions, cancellationToken
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

            if (_plsStore.TradeLogicOptions.EnableAveraging)
            {
                Logger.LogInformation("Averaging is enabled. In {Method}", 
                    nameof(RunInstanceAsync));
                
                await ManageAverageOrdersAsync(instanceResult.Data, cancellationToken);
            }

            if (_plsStore.TradeLogicOptions.EnableOpenPositions)
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
                _plsStore,
                _plsStore.TradeLogicOptions.Leverage, 
                cancellationToken: CancellationTokenSource.Token
            );

            if (changeLeverageResult != ActionResult.Success)
            {
                return changeLeverageResult;
            }
            
            changeLeverageResult = await FuturesUsdEndpoints.ChangeMarginTypeToAllPositionsAsync(
                _plsStore,
                _plsStore.TradeLogicOptions.MarginType, 
                cancellationToken: CancellationTokenSource.Token
            );
            
            if (changeLeverageResult != ActionResult.Success)
            {
                return changeLeverageResult;
            }
            
            var openedPositions = _plsStore.FuturesUsd.AccountData.Positions
                .Where(x => x.EntryPrice != 0)
                .Where(x => x.Quantity != 0);

            foreach (var openedPosition in openedPositions)
            {
                var quantity = openedPosition.PositionSide == PositionSide.Short
                    ? Math.Abs(openedPosition.Quantity)
                    : openedPosition.Quantity;

                var createPositionResult = await _plsPositionWorker.CreatePositionAsync(
                    _plsStore,
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
                
                var openedPosition = _plsStore.Positions
                    .Where(x => x.Name == marketSignals.FuturesUsdName)
                    .SingleOrDefault(x => x.PositionSide == marketSignals.KlinePositionSignal);

                if (openedPosition == null)
                {
                    continue;
                }

                var symbolInfo =
                    _plsStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == openedPosition.Name);

                var lastPrice = _plsStore.MarketLastPrices[openedPosition.Name];
                
                var isAverageNeeded = await _plsFilters.IsNeedToPlaceMarketAverageOrderAsync(instanceResult, openedPosition, lastPrice, 
                    marketSignals, symbolInfo, _plsStore.TradeLogicOptions);
                if (!isAverageNeeded)
                {
                    continue;
                }
                
                var balance = 
                    _plsStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == openedPosition.QuoteAsset);

                await _plsEndpoints.CreateMarketAverageBuyOrderAsync(
                    openedPosition, 
                    lastPrice,
                    symbolInfo,
                    balance,
                    _plsStore.TradeLogicOptions,
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
            var filteredPositions = await _plsFilters.GetFilteredOrdersForOpenPositionAsync(instanceResult, _plsStore.TradeLogicOptions, 
                _plsStore.Positions, _plsStore.FuturesUsd.AccountData.Positions.ToList());
            
            if (_plsStore.Positions.Count >= _plsStore.TradeLogicOptions.MaximumPositions)
            {
                Logger.LogWarning("Cannot open new positions. Opened positions: {OpenedPositions}, available: {AvailablePositions}. In {Method}",
                    _plsStore.Positions.Count, _plsStore.TradeLogicOptions.MaximumPositions, nameof(ManageMarketBuyOrdersAsync));
                
                return;
            }
        
            foreach (var filteredPosition in filteredPositions)
            {
                var positionInfo =
                    _plsStore.FuturesUsd.AccountData.Positions.First(x => x.Symbol == filteredPosition.FuturesUsdName);

                var lastPrice = _plsStore.MarketLastPrices[filteredPosition.FuturesUsdName];
                
                var symbolInfo =
                    _plsStore.FuturesUsd.ExchangerData.ExchangeInfo.Symbols.Single(x => x.Name == filteredPosition.FuturesUsdName);
                
                var balance = _plsStore.FuturesUsd.AccountData.Balances.Single(x => x.Asset == filteredPosition.QuoteAsset);
                
                await _plsEndpoints.CreateBuyMarketOrderAsync(
                    filteredPosition, 
                    lastPrice,
                    symbolInfo,
                    positionInfo,
                    balance,
                    _plsStore.TradeLogicOptions,
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

            var statMessage = MessageGenerator.KlineResultMessage(instanceResult);
            
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