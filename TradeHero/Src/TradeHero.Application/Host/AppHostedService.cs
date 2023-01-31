using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Menu;
using TradeHero.Core.Types.Menu.Models;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Services;
using TradeHero.Core.Types.Trading;

namespace TradeHero.Application.Host;

internal class AppHostedService : IHostedService
{
    private readonly ILogger<AppHostedService> _logger;
    private readonly IJobService _jobService;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly IFileService _fileService;
    private readonly IEnvironmentService _environmentService;
    private readonly IStoreService _storeService;
    private readonly IMenuFactory _menuFactory;
    private readonly IThSocketBinanceClient _binanceSocketClient;
    private readonly ITelegramService _telegramService;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IStrategyRepository _strategyRepository;
    private readonly ITradeLogicFactory _tradeLogicFactory;

    private readonly ApplicationShutdown _applicationShutdown;

    private CancellationTokenSource _cancellationTokenSource = new();
    
    public AppHostedService(
        ILogger<AppHostedService> logger,
        IJobService jobService,
        IInternetConnectionService internetConnectionService,
        IFileService fileService,
        IEnvironmentService environmentService,
        IStoreService storeService,
        IMenuFactory menuFactory, 
        IThSocketBinanceClient binanceSocketClient,
        ITelegramService telegramService,
        IConnectionRepository connectionRepository,
        IStrategyRepository strategyRepository,
        ITradeLogicFactory tradeLogicFactory,
        ApplicationShutdown applicationShutdown
        )
    {
        _logger = logger;
        _jobService = jobService;
        _internetConnectionService = internetConnectionService;
        _fileService = fileService;
        _environmentService = environmentService;
        _storeService = storeService;
        _menuFactory = menuFactory;
        _binanceSocketClient = binanceSocketClient;
        _telegramService = telegramService;
        _connectionRepository = connectionRepository;
        _strategyRepository = strategyRepository;
        _tradeLogicFactory = tradeLogicFactory;
        _applicationShutdown = applicationShutdown;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("App started");
            _logger.LogInformation("Process id: {ProcessId}", _environmentService.GetCurrentProcessId());
            _logger.LogInformation("Application environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
            _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());
            _logger.LogInformation("Runner type: {RunnerType}", _environmentService.GetRunnerType());
            
            _applicationShutdown.SetActionsBeforeStop(StopServicesAsync);
            
            if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
            {
                _logger.LogInformation("Args: {GetBasePath}", string.Join(", ", _environmentService.GetEnvironmentArgs()));   
            }

            await _internetConnectionService.StartInternetConnectionCheckAsync();

            _internetConnectionService.OnInternetConnected += InternetConnectionServiceOnOnInternetConnected;
            _internetConnectionService.OnInternetDisconnected += InternetConnectionServiceOnOnInternetDisconnected;

            RegisterBackgroundJobs();

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.InitAsync(_cancellationTokenSource.Token);   
            }
            
            if (_environmentService.GetEnvironmentArgs().Contains(ArgumentKeyConstants.Update))
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync(
                        $"Application updated to version: {_environmentService.GetCurrentApplicationVersion().ToString(3)}",
                        new SendMessageOptions { MenuAction = MenuAction.MainMenu },
                        cancellationToken
                    );
                }
            }
            else
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Bot is launched!", 
                        new SendMessageOptions { MenuAction = MenuAction.MainMenu }, cancellationToken);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartAsync));
            
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("App stopped");
        
        return Task.CompletedTask;
    }

    #region Private methods

    private async Task StopServicesAsync()
    {
        try
        {
            if (_storeService.Bot.TradeLogic != null)
            {
                await _storeService.Bot.TradeLogic.FinishAsync(false);
                _storeService.Bot.SetTradeLogic(null, TradeLogicStatus.Idle);
            }

            if (!_storeService.Application.Update.IsNeedToUpdateApplication)
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Bot is finished!", 
                        new SendMessageOptions { MenuAction = MenuAction.WithoutMenu }, 
                        _cancellationTokenSource.Token);
                }
            }
            
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.FinishAsync(_cancellationTokenSource.Token);
            }
        
            _jobService.FinishAllJobs();

            await _binanceSocketClient.UnsubscribeAllAsync();
            
            _internetConnectionService.OnInternetConnected -= InternetConnectionServiceOnOnInternetConnected;
            _internetConnectionService.OnInternetDisconnected -= InternetConnectionServiceOnOnInternetDisconnected;
        
            _internetConnectionService.StopInternetConnectionChecking();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StopServicesAsync));
            
            throw;
        }
    }
    
    private void RegisterBackgroundJobs()
    {
        var appSettings = _environmentService.GetAppSettings();
        
        async Task DeleteOldFilesFunction()
        {
            await _fileService.DeleteFilesInFolderAsync(
                Path.Combine(_environmentService.GetBasePath(), appSettings.Folder.LogsFolderName), 
                TimeSpan.FromDays(30).TotalMilliseconds
            );
        }
        
        _jobService.StartJob("DeleteOldLogFiles", DeleteOldFilesFunction, TimeSpan.FromDays(1), true);
    }
    
    private async void InternetConnectionServiceOnOnInternetConnected(object? sender, EventArgs e)
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            var showMessage = false;

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.InitAsync(_cancellationTokenSource.Token);
            }

            while (true)
            {
                if (showMessage)
                {
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("Will try repeat connection in a minute...", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                            _cancellationTokenSource.Token);
                    }
                    
                    await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                }
                else
                {
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("Launched after internet disconnection.", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu }, 
                            _cancellationTokenSource.Token);
                    }
                }

                if (_storeService.Bot.TradeLogic != null)
                {
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("Waiting for closing previous strategy...", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                            _cancellationTokenSource.Token);
                    }
                
                    while (_storeService.Bot.TradeLogic != null) { }

                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("Previous strategy closed.", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                            _cancellationTokenSource.Token);
                    }
                }

                if (_storeService.Bot.TradeLogicStatus == TradeLogicStatus.Running)
                {
                    var connection = await _connectionRepository.GetActiveConnectionAsync();
                    if (connection == null)
                    {
                        foreach (var menu in _menuFactory.GetMenus())
                        {
                            await menu.SendMessageAsync("There is no active connection to exchanger.", 
                                new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                                _cancellationTokenSource.Token);
                        }
                        
                        break;
                    }
                
                    var activeStrategy = await _strategyRepository.GetActiveStrategyAsync();
                    if (activeStrategy == null)
                    {
                        foreach (var menu in _menuFactory.GetMenus())
                        {
                            await menu.SendMessageAsync("There is no active strategy.", 
                                new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                                _cancellationTokenSource.Token);
                        }
                        
                        break;
                    }
                
                    var strategy = _tradeLogicFactory.GetTradeLogicRunner(activeStrategy.TradeLogicType);
                    if (strategy == null)
                    {
                        foreach (var menu in _menuFactory.GetMenus())
                        {
                            await menu.SendMessageAsync("Strategy does not exist", 
                                new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                                _cancellationTokenSource.Token);
                        }

                        break;
                    }
                    
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("In starting process...", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                            _cancellationTokenSource.Token);
                    }
                
                    var strategyResult = await strategy.InitAsync(activeStrategy);
                    if (strategyResult != ActionResult.Success)
                    {
                        await strategy.FinishAsync(true);
                    
                        foreach (var menu in _menuFactory.GetMenus())
                        {
                            await menu.SendMessageAsync(
                                $"Cannot start '{activeStrategy.Name}' strategy. Error code: {strategyResult}", 
                                new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                                _cancellationTokenSource.Token);
                        }

                        showMessage = true;
                    
                        continue;
                    }
                
                    _storeService.Bot.SetTradeLogic(strategy, TradeLogicStatus.Running);
                
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("Strategy started! Enjoy lazy pidor", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                            _cancellationTokenSource.Token);
                    }

                    break;
                }
            
                foreach (var menu in _menuFactory.GetMenus())
                {
                    if (menu.MenuType == MenuType.Telegram)
                    {
                        await menu.SendMessageAsync("Choose action:", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu },
                            _cancellationTokenSource.Token);
                    }
                }

                break;
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(InternetConnectionServiceOnOnInternetConnected));
        }
    }
    
    private async void InternetConnectionServiceOnOnInternetDisconnected(object? sender, EventArgs e)
    {
        try
        {
            _cancellationTokenSource.Cancel();

            if (_storeService.Bot.TradeLogic != null)
            {
                _internetConnectionService.SetPauseInternetConnectionChecking(true);
            
                await _storeService.Bot.TradeLogic.FinishAsync(true);
            
                _storeService.Bot.SetTradeLogic(null, TradeLogicStatus.Running);
            
                _internetConnectionService.SetPauseInternetConnectionChecking(false);
            }
        
            await _telegramService.CloseConnectionAsync();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(InternetConnectionServiceOnOnInternetDisconnected));
        }
    }

    #endregion
}