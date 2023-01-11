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
using TradeHero.Trading.TradeLogic.PercentMove.Flow;
using TradeHero.Trading.TradeLogic.PercentMove.Streams;

namespace TradeHero.Trading.TradeLogic.PercentMove;

internal class PercentMoveTradeLogic : BaseFuturesUsdTradeLogic
{
    private readonly PercentMovePositionWorker _percentMovePositionWorker;
    private readonly PercentMoveStore _percentMoveStore;

    public PercentMoveTradeLogic(
        ILogger<PercentMoveTradeLogic> logger,
        IThSocketBinanceClient binanceSocketClient,
        ITelegramService telegramService,
        IJobService jobService,
        IFuturesUsdEndpoints futuresUsdEndpoints,
        ISpotEndpoints spotEndpoints,
        IInstanceFactory instanceFactory,
        IFuturesUsdMarketTickerStream futuresUsdMarketTickerStream,
        PercentMovePositionWorker percentMovePositionWorker,
        PercentMoveStore percentMoveStore, 
        PercentMoveUserAccountStream percentMoveUserAccountStream
        )
        : base(binanceSocketClient, jobService, spotEndpoints, instanceFactory, futuresUsdMarketTickerStream,
            percentMoveUserAccountStream, percentMovePositionWorker, logger, telegramService, futuresUsdEndpoints)
    {
        _percentMovePositionWorker = percentMovePositionWorker;
        _percentMoveStore = percentMoveStore;

        Store = _percentMoveStore;
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
            
            var openedPositions = _percentMoveStore.FuturesUsd.AccountData.Positions
                .Where(x => x.EntryPrice != 0)
                .Where(x => x.Quantity != 0);

            foreach (var openedPosition in openedPositions)
            {
                var quantity = openedPosition.PositionSide == PositionSide.Short
                    ? Math.Abs(openedPosition.Quantity)
                    : openedPosition.Quantity;

                var createPositionResult = await _percentMovePositionWorker.CreatePositionAsync(
                    _percentMoveStore,
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
                _percentMoveStore, instanceOptions, cancellationToken
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