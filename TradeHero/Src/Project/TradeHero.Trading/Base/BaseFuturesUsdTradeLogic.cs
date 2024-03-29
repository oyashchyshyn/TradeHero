using Binance.Net.Interfaces.Clients;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Args;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Contracts.Trading;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Repositories;
using TradeHero.Core.Models.Trading;
using TradeHero.Trading.Constants;
using TradeHero.Trading.Endpoints.Rest;

namespace TradeHero.Trading.Base;

internal abstract class BaseFuturesUsdTradeLogic : ITradeLogic
{
    private readonly IBinanceSocketClient _binanceSocketClient;
    private readonly IJobService _jobService;
    private readonly ISpotEndpoints _spotEndpoints;
    private readonly IInstanceFactory _instanceFactory;
    
    private readonly BaseFuturesUsdUserAccountStream _userAccountStreamStream;

    protected readonly ILogger Logger;
    protected readonly ITelegramService TelegramService;
    protected readonly IFuturesUsdEndpoints FuturesUsdEndpoints;
    protected IInstance? Instance;

    protected readonly CancellationTokenSource CancellationTokenSource = new();

    public event EventHandler<FuturesUsdOrderReceiveArgs>? OnOrderReceive;
    
    public ITradeLogicStore Store { get; protected init; } = null!;

    protected BaseFuturesUsdTradeLogic(
        IBinanceSocketClient binanceSocketClient,
        IJobService jobService,
        ISpotEndpoints spotEndpoints,
        IInstanceFactory instanceFactory,
        BaseFuturesUsdUserAccountStream userAccountStreamStream,
        ILogger logger,
        ITelegramService telegramService,
        IFuturesUsdEndpoints futuresUsdEndpoints
        )
    {
        _binanceSocketClient = binanceSocketClient;
        _jobService = jobService;
        _spotEndpoints = spotEndpoints;
        _instanceFactory = instanceFactory;
        _userAccountStreamStream = userAccountStreamStream;

        Logger = logger;
        TelegramService = telegramService;
        FuturesUsdEndpoints = futuresUsdEndpoints;
    }
    
    protected abstract Task RunInstanceAsync(BaseInstanceOptions instanceOptions, CancellationToken cancellationToken);
    protected abstract Task<ActionResult> CheckCurrentPositionsAsync(CancellationToken cancellationToken); 

    public async Task<ActionResult> InitAsync(StrategyDto strategyDto)
    {
        try
        {
            if (CancellationTokenSource.Token.IsCancellationRequested)
            {
                Logger.LogInformation("CancellationToken is requested. In {Method}",
                    nameof(InitAsync));
                
                return ActionResult.CancellationTokenRequested;
            }

            var actionResult = ((BaseTradeLogicStore)Store).AddTradeLogicOptions(strategyDto);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }

            actionResult = await FuturesUsdEndpoints.SetFuturesUsdWalletBalancesAsync(Store, cancellationToken: CancellationTokenSource.Token);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }
        
            actionResult = await _spotEndpoints.SetSpotExchangeInfoAsync(Store, cancellationToken: CancellationTokenSource.Token);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }
        
            actionResult = await FuturesUsdEndpoints.SetFuturesUsdExchangeInfoAsync(Store, cancellationToken: CancellationTokenSource.Token);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }
        
            actionResult = await FuturesUsdEndpoints.SetFuturesUsdPositionInfoAsync(Store, cancellationToken: CancellationTokenSource.Token);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }

            actionResult = await FuturesUsdEndpoints.SetFuturesUsdStreamListenKeyAsync(Store, cancellationToken: CancellationTokenSource.Token);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }

            actionResult = await _userAccountStreamStream.StartUserUpdateDataStreamAsync(OnOrderReceive);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }

            actionResult = await CheckCurrentPositionsAsync(CancellationTokenSource.Token);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }

            SetBackgroundJobs(Store, CancellationTokenSource.Token);

            if (strategyDto.InstanceType == InstanceType.NoInstance)
            {
                return ActionResult.Success;
            }
            
            var instanceResult = _instanceFactory.GetInstance(strategyDto.InstanceType);
            if (instanceResult != null)
            {
                Instance = instanceResult.Instance;
                
                actionResult = ((BaseTradeLogicStore)Store).AddInstanceOptions(strategyDto, instanceResult.Type);
                if (actionResult != ActionResult.Success)
                {
                    return actionResult;
                }
                
                await RunInstancesAsync(CancellationTokenSource.Token);
            }
            else
            {
                Logger.LogWarning("{PropertyName} is null. {InstanceType} is {InstanceValue}. In {Method}", 
                    nameof(instanceResult), nameof(strategyDto.InstanceType), strategyDto.InstanceType, nameof(InitAsync));
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(InitAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(InitAsync));
            
            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> UpdateTradeSettingsAsync(StrategyDto strategyDto)
    {
        try
        {
            _jobService.FinishJobByKey(JobKey.RunInstanceForStrategy);

            await ((BaseTradeLogicStore)Store).ClearInstanceOptionsAsync();
            
            var actionResult = ((BaseTradeLogicStore)Store).AddTradeLogicOptions(strategyDto);
            if (actionResult != ActionResult.Success)
            {
                return actionResult;
            }

            var instanceResult = _instanceFactory.GetInstance(strategyDto.InstanceType);
            if (instanceResult != null)
            {
                Instance = instanceResult.Instance;
                
                actionResult = ((BaseTradeLogicStore)Store).AddInstanceOptions(strategyDto, instanceResult.Type);
                if (actionResult != ActionResult.Success)
                {
                    return actionResult;
                }
                
                await RunInstancesAsync(CancellationTokenSource.Token);   
            }
            else
            {
                Logger.LogWarning("{PropertyName} is null. {InstanceType} is {InstanceValue}. In {Method}", 
                    nameof(instanceResult), nameof(strategyDto.InstanceType), strategyDto.InstanceType, nameof(InitAsync));
            }
            
            Logger.LogInformation("Update strategy settings finished. In {Method}", nameof(UpdateTradeSettingsAsync));
            
            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(UpdateTradeSettingsAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(UpdateTradeSettingsAsync));

            return ActionResult.SystemError;
        }
    }
    
    public async Task<ActionResult> FinishAsync(bool isNeedToUseCancellationToken)
    {
        try
        {
            if (isNeedToUseCancellationToken)
            {
                CancellationTokenSource.Cancel();
            }
            
            _jobService.FinishJobByKey(JobKey.RunInstanceForStrategy);
            _jobService.FinishJobByKey(JobKey.UpdateStreamListenKeyInStore);
            _jobService.FinishJobByKey(JobKey.UpdateExchangeInfoInStore);
            _jobService.FinishJobByKey(JobKey.UpdateBalancesInfoInStore);
            _jobService.FinishJobByKey(JobKey.UpdatePositionsInfoInStore);

            foreach (var tickerStreamKeyValue in Store.SymbolTickerStreams)
            {
                if (tickerStreamKeyValue.Value == null)
                {
                    Logger.LogWarning("{Symbol}. Ticker stream is null. In {Method}",
                        tickerStreamKeyValue.Key, nameof(FinishAsync));
                    
                    continue;
                }

                await _binanceSocketClient.UnsubscribeAsync(tickerStreamKeyValue.Value.SocketSubscription);
                
                Logger.LogInformation("{Symbol}. Socket ticker unsubscribed. In {Method}", 
                    tickerStreamKeyValue.Key, nameof(FinishAsync));
            }

            await _binanceSocketClient.UnsubscribeAsync(_userAccountStreamStream.SocketSubscription);
            Logger.LogInformation("Unsubscribe from Account Stream. In {Method}", nameof(FinishAsync));
            
            await FuturesUsdEndpoints.DestroyStreamListerKeyAsync(Store, cancellationToken: CancellationTokenSource.Token);

            await ((BaseTradeLogicStore)Store).ClearDataAsync();

            Logger.LogInformation("Finished data from strategy. In {Method}", nameof(FinishAsync));
            
            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(FinishAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(FinishAsync));

            return ActionResult.SystemError;
        }
    }

    #region Private methods

    private async Task RunInstancesAsync(CancellationToken cancellationToken)
    {
        var instance = ((BaseTradeLogicStore)Store).InstanceOptions;
        
        if (instance == null)
        {
            Logger.LogInformation("Instance is null. In {Method}", 
                nameof(RunInstanceAsync));
            
            return;
        }
        
        if (instance.TelegramChannelId.HasValue && !string.IsNullOrWhiteSpace(instance.TelegramChannelName))
        {
            await TelegramService.ChangeChannelTitleAsync(instance.TelegramChannelId.Value, 
                instance.TelegramChannelName, cancellationToken);
        }
                
        async Task StrategyLogicJob()
        {
            await RunInstanceAsync(instance, cancellationToken);
        }

        _jobService.StartJob(JobKey.RunInstanceForStrategy, StrategyLogicJob, 
            instance.Interval, instance.RunImmediately);

        Logger.LogInformation("Registered instance: {Instance}. In {Method}", 
            instance.ToString(), nameof(RunInstanceAsync));
    }
    
    private void SetBackgroundJobs(ITradeLogicStore store, CancellationToken cancellationToken)
    {
        async Task UpdateStreamListenKeyInStoreJob()
        {
            await FuturesUsdEndpoints.UpdateStreamListerKeyAsync(store, cancellationToken: cancellationToken);
        }
        _jobService.StartJob(JobKey.UpdateStreamListenKeyInStore, UpdateStreamListenKeyInStoreJob, delay: TimeSpan.FromMinutes(20));
        
        async Task UpdateExchangeInfoInStoreJob()
        {
            await _spotEndpoints.SetSpotExchangeInfoAsync(store, cancellationToken: cancellationToken);
            await FuturesUsdEndpoints.SetFuturesUsdExchangeInfoAsync(store, cancellationToken: cancellationToken);
        }
        _jobService.StartJob(JobKey.UpdateExchangeInfoInStore, UpdateExchangeInfoInStoreJob, delay: TimeSpan.FromMinutes(5));

        async Task UpdatePositionsInfoInStoreJob()
        {
            await FuturesUsdEndpoints.SetFuturesUsdPositionInfoAsync(store, cancellationToken: cancellationToken);
        }
        _jobService.StartJob(JobKey.UpdatePositionsInfoInStore, UpdatePositionsInfoInStoreJob, delay: TimeSpan.FromMinutes(3));
        
        async Task UpdateBalancesInfoInStoreJob()
        {
            await FuturesUsdEndpoints.SetFuturesUsdWalletBalancesAsync(store, cancellationToken: cancellationToken);
        }
        _jobService.StartJob(JobKey.UpdateBalancesInfoInStore, UpdateBalancesInfoInStoreJob, delay: TimeSpan.FromSeconds(15));
    }

    #endregion
}