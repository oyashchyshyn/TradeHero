using Microsoft.Extensions.Logging;
using TradeHero.Core.Args;
using TradeHero.Core.Constants;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Repositories;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Contracts.Trading;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Menu;
using TradeHero.Core.Models.Terminal;

namespace TradeHero.Application.Bot;

internal class BotWorker
{
    private readonly ILogger<BotWorker> _logger;
    private readonly IMenuFactory _menuFactory;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IStoreService _storeService;
    private readonly IEnvironmentService _environmentService;
    private readonly ITradeLogicFactory _tradeLogicFactory;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly ITerminalService _terminalService;
    private readonly IDateTimeService _dateTimeService;
    
    private readonly ManualResetEventSlim _internetConnectionResetEvent = new(true);
    private readonly Mutex _positionConsoleNotificationMutex = new();
    
    private CancellationTokenSource _cancellationTokenSource = new();

    public BotWorker(
        ILogger<BotWorker> logger, 
        IMenuFactory menuFactory, 
        IStrategyRepository strategyRepository, 
        IConnectionRepository connectionRepository, 
        IStoreService storeService, 
        IEnvironmentService environmentService, 
        ITradeLogicFactory tradeLogicFactory, 
        IInternetConnectionService internetConnectionService, 
        ITerminalService terminalService, 
        IDateTimeService dateTimeService
        )
    {
        _logger = logger;
        _menuFactory = menuFactory;
        _strategyRepository = strategyRepository;
        _connectionRepository = connectionRepository;
        _storeService = storeService;
        _environmentService = environmentService;
        _tradeLogicFactory = tradeLogicFactory;
        _internetConnectionService = internetConnectionService;
        _terminalService = terminalService;
        _dateTimeService = dateTimeService;
    }

    public async Task InitBotAsync()
    {
        _internetConnectionService.OnInternetConnected += InternetConnectionServiceOnOnInternetConnected;
        _internetConnectionService.OnInternetDisconnected += InternetConnectionServiceOnOnInternetDisconnected;
            
        foreach (var menu in _menuFactory.GetMenus())
        {
            await menu.InitAsync(_cancellationTokenSource.Token);   
        }
        
        if (_environmentService.GetEnvironmentArgs().Contains(ArgumentKeyConstants.Update))
        {
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync(
                    $"Application updated to version: {_environmentService.GetCurrentApplicationVersion().ToString(3)}.",
                    new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token
                );
            }
        }
        else
        {
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync("Bot is launched!", 
                    new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true }, 
                    _cancellationTokenSource.Token);
            }
        }
            
        _storeService.Bot.OnTradeLogicUpdate += BotOnOnTradeLogicUpdate;
    }

    public async Task FinishBotAsync()
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
                    new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }
        }
            
        foreach (var menu in _menuFactory.GetMenus())
        {
            await menu.FinishAsync(_cancellationTokenSource.Token);
        }
            
        _internetConnectionResetEvent.Dispose();
        _positionConsoleNotificationMutex.Dispose();
    }
    
    public async Task StartTradeLogicAsync()
    {
        try
        {
            var activeStrategy = await _strategyRepository.GetActiveStrategyAsync();
            if (activeStrategy == null)
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("There is no active strategy.", 
                        new SendMessageOptions { MenuAction = MenuAction.PreviousMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }
                
                return;
            }
            
            var connection = await _connectionRepository.GetActiveConnectionAsync();
            if (connection == null)
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("There is no active connection to exchanger.", 
                        new SendMessageOptions { MenuAction = MenuAction.PreviousMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }

                return;
            }
            
            var tradeLogic = _tradeLogicFactory.GetTradeLogicRunner(activeStrategy.TradeLogicType);
            if (tradeLogic == null)
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Strategy does not exist.", 
                        new SendMessageOptions { MenuAction = MenuAction.PreviousMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }
                
                return;
            }

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync("In starting process...", 
                    new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }

            var strategyResult = await tradeLogic.InitAsync(activeStrategy);
            if (strategyResult != ActionResult.Success)
            {
                await tradeLogic.FinishAsync(true);
                
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync($"Cannot start '{activeStrategy.Name}' strategy. Error code: {strategyResult}.", 
                        new SendMessageOptions { MenuAction = MenuAction.PreviousMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }

                return;
            }
        
            _storeService.Bot.SetTradeLogic(tradeLogic, TradeLogicStatus.Running);
        
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync("Strategy started! Enjoy lazy pidor.", 
                    new SendMessageOptions { MenuAction = MenuAction.PreviousMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(StartTradeLogicAsync));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartTradeLogicAsync));
            
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync("There was an error during process, please, try later.", 
                    new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }
        }
    }
    
    public async Task StopTradeLogicAsync()
    {
        try
        {
            if (_storeService.Bot.TradeLogic == null)
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Cannot stop strategy because it does not running.", 
                        new SendMessageOptions { MenuAction = MenuAction.PreviousMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }
                
                return;
            }

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync("Stopping...", 
                    new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }
            
            var stopResult = await _storeService.Bot.TradeLogic.FinishAsync(true);
            if (stopResult != ActionResult.Success)
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Error during stopping strategy.", 
                        new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }

                return;
            }
            
            _storeService.Bot.SetTradeLogic(null, TradeLogicStatus.Idle);

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync("Strategy stopped.", 
                    new SendMessageOptions { MenuAction = MenuAction.PreviousMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(StopTradeLogicAsync));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StopTradeLogicAsync));
            
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync("There was an error during process, please, try later.", 
                    new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }
        }
    }

    #region Private methods

    private async void InternetConnectionServiceOnOnInternetConnected(object? sender, EventArgs eventArgs)
    {
        try
        {
            _internetConnectionResetEvent.Wait();
            
            _cancellationTokenSource = new CancellationTokenSource();

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.InitAsync(_cancellationTokenSource.Token);
                
                await menu.SendMessageAsync("Launched after internet disconnection.", 
                    new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }

            if (_storeService.Bot.TradeLogic != null)
            {
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Waiting for closing previous strategy...", 
                        new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }
                
                while (_storeService.Bot.TradeLogic != null) { }

                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Previous strategy closed.", 
                        new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
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
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                            _cancellationTokenSource.Token);
                    }
                        
                    return;
                }
                
                var activeStrategy = await _strategyRepository.GetActiveStrategyAsync();
                if (activeStrategy == null)
                {
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("There is no active strategy.", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                            _cancellationTokenSource.Token);
                    }
                        
                    return;
                }
                
                var strategy = _tradeLogicFactory.GetTradeLogicRunner(activeStrategy.TradeLogicType);
                if (strategy == null)
                {
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync("Strategy does not exist.", 
                            new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                            _cancellationTokenSource.Token);
                    }

                    return;
                }
                    
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("In starting process...", 
                        new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }
                
                var strategyResult = await strategy.InitAsync(activeStrategy);
                if (strategyResult != ActionResult.Success)
                {
                    await strategy.FinishAsync(true);
                    
                    foreach (var menu in _menuFactory.GetMenus())
                    {
                        await menu.SendMessageAsync(
                            $"Cannot start '{activeStrategy.Name}' strategy. Error code: {strategyResult}.", 
                            new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                            _cancellationTokenSource.Token);
                    }
                        
                    return;
                }
                
                _storeService.Bot.SetTradeLogic(strategy, TradeLogicStatus.Running);
                
                foreach (var menu in _menuFactory.GetMenus())
                {
                    await menu.SendMessageAsync("Strategy started! Enjoy lazy pidor.", 
                        new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }

                return;
            }
            
            foreach (var menu in _menuFactory.GetMenus())
            {
                if (menu.MenuType == MenuType.Telegram)
                {
                    await menu.SendMessageAsync("Choose action:", 
                        new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);
                }
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(InternetConnectionServiceOnOnInternetConnected));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(InternetConnectionServiceOnOnInternetConnected));
            
            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.SendMessageAsync(
                    "There was an error during bot starting. Please, try again later.", 
                    new SendMessageOptions { MenuAction = MenuAction.MainMenu, IsNeedToShowTime = true },
                    _cancellationTokenSource.Token);
            }
        }
    }
    
    private async void InternetConnectionServiceOnOnInternetDisconnected(object? sender, EventArgs eventArgs)
    {
        try
        {
            _internetConnectionResetEvent.Reset();
            
            foreach (var menu in _menuFactory.GetMenus())
            {
                if (menu.MenuType == MenuType.Console)
                {
                    await menu.SendMessageAsync("Internet connection disconnected.", 
                        new SendMessageOptions { MenuAction = MenuAction.WithoutMenu, IsNeedToShowTime = true },
                        _cancellationTokenSource.Token);   
                }
            }

            _cancellationTokenSource.Cancel();

            if (_storeService.Bot.TradeLogic != null)
            {
                await _storeService.Bot.TradeLogic.FinishAsync(true);

                _storeService.Bot.SetTradeLogic(null, TradeLogicStatus.Running);
            }

            foreach (var menu in _menuFactory.GetMenus())
            {
                await menu.FinishAsync(_cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(InternetConnectionServiceOnOnInternetDisconnected));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}",
                nameof(InternetConnectionServiceOnOnInternetDisconnected));
        }
        finally
        {
            _internetConnectionResetEvent.Set();
        }
    }

    private void BotOnOnTradeLogicUpdate(object? sender, EventArgs eventArgs)
    {
        try
        {
            if (_storeService.Bot.TradeLogic != null)
            {
                _storeService.Bot.TradeLogic.OnOrderReceive += TradeLogicOnOnOrderReceive;
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}",
                nameof(BotOnOnTradeLogicUpdate));
        }
    }

    private void TradeLogicOnOnOrderReceive(object? sender, FuturesUsdOrderReceiveArgs eventArgs)
    {
        try
        {
            _positionConsoleNotificationMutex.WaitOne();
            
            if (_storeService.Bot.TradeLogic == null)
            {
                _logger.LogInformation("{PropertyName} is null. In {Method}", 
                    nameof(_storeService.Bot.TradeLogic), nameof(TradeLogicOnOnOrderReceive));
                
                return;
            }

            var symbolInfo = _storeService.Bot.TradeLogic.Store.FuturesUsd.ExchangerData.ExchangeInfo.Symbols
                .FirstOrDefault(x => x.Name == eventArgs.OrderUpdate.Symbol);

            if (symbolInfo == null)
            {
                _logger.LogInformation("{PropertyName} is null. In {Method}", 
                    nameof(symbolInfo), nameof(TradeLogicOnOnOrderReceive));
                
                return;
            }

            string orderType;
            ConsoleColor orderTyreForegroundColor;

            switch (eventArgs.OrderReceiveType)
            {
                case OrderReceiveType.Open:
                    orderType = "POSITION OPEN";
                    orderTyreForegroundColor = ConsoleColor.Green;
                    break;
                case OrderReceiveType.Average:
                    orderType = "POSITION AVERAGE";
                    orderTyreForegroundColor = ConsoleColor.Yellow;
                    break;
                case OrderReceiveType.PartialClosed:
                    orderType = "POSITION PARTIAL CLOSE";
                    orderTyreForegroundColor = ConsoleColor.Red;
                    break;
                case OrderReceiveType.FullyClosed:
                    orderType = "POSITION FULL CLOSE";
                    orderTyreForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    return;
            }

            _terminalService.Write($"[{_dateTimeService.GetLocalDateTime():HH:mm:ss}]", new WriteMessageOptions { FontColor = ConsoleColor.Gray });
            _terminalService.Write(" ");
            _terminalService.Write(orderType, new WriteMessageOptions { FontColor = orderTyreForegroundColor });
            _terminalService.Write(" ");
            _terminalService.Write(eventArgs.OrderUpdate.Symbol);
            _terminalService.Write(" ");
            _terminalService.Write($"QUANTITY: {eventArgs.OrderUpdate.Quantity}");
            _terminalService.Write(" ");
            _terminalService.Write($"QUOTE: {Math.Round(eventArgs.OrderUpdate.Price * eventArgs.OrderUpdate.Quantity, 2)} {symbolInfo.QuoteAsset}", 
                new WriteMessageOptions { IsMessageFinished = true});
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}",
                nameof(TradeLogicOnOnOrderReceive));
        }
        finally
        {
            _positionConsoleNotificationMutex.ReleaseMutex();
        }
    }
    
    #endregion
}