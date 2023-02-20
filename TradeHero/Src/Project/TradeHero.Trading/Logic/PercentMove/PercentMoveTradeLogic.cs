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
using TradeHero.Trading.Logic.PercentMove.Flow;
using TradeHero.Trading.Logic.PercentMove.Streams;

namespace TradeHero.Trading.Logic.PercentMove;

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
        PercentMovePositionWorker percentMovePositionWorker,
        PercentMoveStore percentMoveStore, 
        PercentMoveUserAccountStream percentMoveUserAccountStream
        )
        : base(binanceSocketClient, jobService, spotEndpoints, instanceFactory,
            percentMoveUserAccountStream, logger, telegramService, futuresUsdEndpoints)
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
                Logger.LogInformation("Cancellation token is requested. In {Method}", 
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
            Logger.LogInformation("{Message}. In {Method}",
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
                Logger.LogInformation("CancellationToken is requested. In {Method}",
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
            Logger.LogInformation("{Message}. In {Method}",
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
                Logger.LogInformation("Cancellation token is requested. In {Method}", 
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
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(SendMessageAsync));
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(SendMessageAsync));
        }
    }

    #endregion
}