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
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Flow;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Streams;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy;

internal class PmsStrategy : BaseFuturesUsdStrategy
{
    private readonly PmsPositionWorker _pmsPositionWorker;
    private readonly PmsStore _pmsStore;

    public PmsStrategy(
        ILogger<PmsStrategy> logger,
        IThSocketBinanceClient binanceSocketClient,
        ITelegramService telegramService,
        IJobService jobService,
        IFuturesUsdEndpoints futuresUsdEndpoints,
        ISpotEndpoints spotEndpoints,
        IInstanceFactory instanceFactory,
        IFuturesUsdMarketTickerStream futuresUsdMarketTickerStream,
        PmsPositionWorker pmsPositionWorker,
        PmsStore pmsStore, 
        PmsUserAccountStream pmsUserAccountStream
        )
        : base(binanceSocketClient, jobService, spotEndpoints, instanceFactory, futuresUsdMarketTickerStream,
            pmsUserAccountStream, pmsPositionWorker, logger, telegramService, futuresUsdEndpoints)
    {
        _pmsPositionWorker = pmsPositionWorker;
        _pmsStore = pmsStore;

        Store = _pmsStore;
    }
    
    protected override async Task<ActionResult> CheckCurrentPositionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogWarning("Cancellation token is requested. In {Method}", 
                    nameof(CheckCurrentPositionsAsync));

                return ActionResult.CancellationTokenRequested;
            }
            
            var openedPositions = _pmsStore.FuturesUsd.AccountData.Positions
                .Where(x => x.EntryPrice != 0)
                .Where(x => x.Quantity != 0);

            foreach (var openedPosition in openedPositions)
            {
                var quantity = openedPosition.PositionSide == PositionSide.Short
                    ? Math.Abs(openedPosition.Quantity)
                    : openedPosition.Quantity;

                var createPositionResult = await _pmsPositionWorker.CreatePositionAsync(
                    _pmsStore,
                    openedPosition.Symbol,
                    openedPosition.PositionSide,
                    openedPosition.EntryPrice,
                    openedPosition.UpdateTime,
                    quantity,
                    true,
                    CancellationTokenSource.Token
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
            
            return ActionResult.SystemError;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(CheckCurrentPositionsAsync));

            return ActionResult.SystemError;
        }
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
                _pmsStore, instanceOptions, cancellationToken
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

            if (instanceOptions is { TelegramChannelId: { }, TelegramIsNeedToSendMessages: { } } 
                && instanceOptions.TelegramIsNeedToSendMessages.Value)
            {
                Logger.LogInformation("Preparing positions messaged for telegram. In {Method}", 
                    nameof(RunInstanceAsync));
                
                await SendMessageAsync(instanceResult.Data, instanceOptions.TelegramChannelId.Value, cancellationToken);   
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
    
    #region Private emthods

    private async Task SendMessageAsync(InstanceResult instanceResult, long channelId, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogWarning("Cancellation token is requested. In {Method}", 
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