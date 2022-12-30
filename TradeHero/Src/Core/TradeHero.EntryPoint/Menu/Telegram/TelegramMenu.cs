using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Telegram;
using TradeHero.Contracts.Store;
using TradeHero.Contracts.Strategy;
using TradeHero.EntryPoint.Menu.Telegram.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Bot;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Bot.Commands;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Positions;
using TradeHero.EntryPoint.Menu.Telegram.Commands.StartStop;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Strategy;
using TradeHero.EntryPoint.Menu.Telegram.Commands.Strategy.Commands;

namespace TradeHero.EntryPoint.Menu.Telegram;

internal class TelegramMenu : IMenuService
{
    private readonly ILogger<TelegramMenu> _logger;
    private readonly IStore _store;
    private readonly ITelegramService _telegramService;
    private readonly IInternetConnectionService _internetConnectionService;
    private readonly IStrategyFactory _strategyFactory;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IJsonService _jsonService;

    private readonly TelegramMenuStore _telegramMenuStore;
    
    private readonly List<IMenuCommand> _commands = new();

    public TelegramMenu(
        ILogger<TelegramMenu> logger,
        IServiceProvider serviceProvider, 
        IStore store,
        ITelegramService telegramService, 
        IInternetConnectionService internetConnectionService,
        IStrategyFactory strategyFactory,
        IStrategyRepository strategyRepository,
        IJsonService jsonService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _store = store;
        _telegramService = telegramService;
        _internetConnectionService = internetConnectionService;
        _strategyFactory = strategyFactory;
        _strategyRepository = strategyRepository;
        _jsonService = jsonService;
        _telegramMenuStore = telegramMenuStore;

        _commands.AddRange(new List<IMenuCommand>
        {
            serviceProvider.GetRequiredService<MainMenuCommand>(),
            serviceProvider.GetRequiredService<BotCommand>(),
            serviceProvider.GetRequiredService<StartStrategyCommand>(),
            serviceProvider.GetRequiredService<StopStrategyCommand>(),
            serviceProvider.GetRequiredService<CheckCodeStatusCommand>(),
            serviceProvider.GetRequiredService<PidorCommand>(),
            serviceProvider.GetRequiredService<PositionsCommand>(),
            serviceProvider.GetRequiredService<StrategyCommand>(),
            serviceProvider.GetRequiredService<ShowStrategiesCommand>(),
            serviceProvider.GetRequiredService<ShowStrategiesPropertiesCommand>(),
            serviceProvider.GetRequiredService<AddStrategyCommand>(),
            serviceProvider.GetRequiredService<UpdateStrategyCommand>(),
            serviceProvider.GetRequiredService<SetActiveStrategyCommand>(),
            serviceProvider.GetRequiredService<DeleteStrategyCommand>()
        });
    }

    public async Task<ActionResult> InitAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await InitTelegramMenuAsync(cancellationToken);
            if (result == ActionResult.Success)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Bot is launched!", 
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.MainMenu),
                    cancellationToken: cancellationToken
                );   
            }

            return result;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(InitAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(InitAsync));
            
            return ActionResult.SystemError;
        }
    }

    public async Task<ActionResult> FinishAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramService.OnTelegramBotUserChatUpdate -= TelegramServiceOnOnTelegramBotUserChatUpdate;
            _telegramMenuStore.LastCommandId = string.Empty;
            
            if (_store.Bot.Strategy != null)
            {
                await _store.Bot.Strategy.FinishAsync(false);
                _store.Bot.SetStrategy(null, StrategyStatus.Idle);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ActionResult.Success;
            }
            
            await _telegramService.SendTextMessageToUserAsync(
                "Bot is finished!", 
                _telegramMenuStore.GetRemoveKeyboard(),
                cancellationToken: cancellationToken
            );
                
            await _telegramService.CloseConnectionAsync();

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(FinishAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(FinishAsync));
            
            return ActionResult.SystemError;
        }
    }

    public async Task OnDisconnectFromInternetAsync()
    {
        if (_store.Bot.Strategy != null)
        {
            _internetConnectionService.SetPauseInternetConnectionChecking(true);
            
            await _store.Bot.Strategy.FinishAsync(true);
            
            _store.Bot.SetStrategy(null, StrategyStatus.Running);
            
            _internetConnectionService.SetPauseInternetConnectionChecking(false);
        }
        
        await _telegramService.CloseConnectionAsync();
    }
    
    public async Task OnReconnectToInternetAsync(CancellationToken cancellationToken)
    {
        var showMessage = false;

        var result = await InitTelegramMenuAsync(cancellationToken);
        while (result != ActionResult.Success)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            
            _logger.LogError("Cannot init telegram services. In {Method}", 
                nameof(OnReconnectToInternetAsync));
            
            result = await InitAsync(cancellationToken);
        }
        
        while (true)
        {
            if (showMessage)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Will try repeat connection in a minute...", 
                    _telegramMenuStore.GetRemoveKeyboard(), 
                    cancellationToken: cancellationToken
                );

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            else
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Launched after internet disconnection.", 
                    _telegramMenuStore.GetRemoveKeyboard(), 
                    cancellationToken: cancellationToken
                );
            }

            if (_store.Bot.Strategy != null)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Waiting for closing previous strategy...", 
                    _telegramMenuStore.GetRemoveKeyboard(), 
                    cancellationToken: cancellationToken
                );
                
                while (_store.Bot.Strategy != null) { }
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Previous strategy closed.", 
                    _telegramMenuStore.GetRemoveKeyboard(), 
                    cancellationToken: cancellationToken
                );
            }

            if (_store.Bot.StrategyStatus == StrategyStatus.Running)
            {
                var activeStrategy = await _strategyRepository.GetActiveStrategyAsync();
                if (activeStrategy == null)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "There is no active strategy.", 
                        _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.MainMenu),
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }
                
                var strategy = _strategyFactory.GetStrategy(activeStrategy.StrategyType);
                if (strategy == null)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Strategy does not exist", 
                        _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.MainMenu),
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                await _telegramService.SendTextMessageToUserAsync(
                    "In starting process...", 
                    _telegramMenuStore.GetRemoveKeyboard(), 
                    cancellationToken: cancellationToken
                );
                
                var strategyResult = await strategy.InitAsync(activeStrategy);
                if (strategyResult != ActionResult.Success)
                {
                    await strategy.FinishAsync(true);
                    
                    await _telegramService.SendTextMessageToUserAsync(
                        $"Cannot start '{activeStrategy.Name}' strategy. Error code: {strategyResult}", 
                        _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.MainMenu),
                        cancellationToken: cancellationToken
                    );
                    
                    showMessage = true;
                    
                    continue;
                }
                
                _store.Bot.SetStrategy(strategy, StrategyStatus.Running);
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Strategy started! Enjoy lazy pidor", 
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.MainMenu),
                    cancellationToken: cancellationToken
                );
                
                return;
            }
            
            await _telegramService.SendTextMessageToUserAsync(
                "Choose action:", 
                _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.MainMenu),
                cancellationToken: cancellationToken
            );

            break;
        }
    }
    
    #region Priuvate methods

    private async void TelegramServiceOnOnTelegramBotUserChatUpdate(object? sender, OnTelegramBotUpdateEventArgs args)
    {
        _logger.LogInformation("Message from user. Data: {Data}. In {Method}", 
            _jsonService.SerializeObject(args).Data, nameof(TelegramServiceOnOnTelegramBotUserChatUpdate));
        
        if (args.CallbackQuery is { Data: { } })
        {
            var lastCommand = _commands.SingleOrDefault(menuCommand => menuCommand.Id == _telegramMenuStore.LastCommandId);
            if (lastCommand == null)
            {
                return;
            }
            
            await lastCommand.HandleCallbackDataAsync(args.CallbackQuery.Data, args.CancellationToken);
            
            return;
        }
        
        if (string.IsNullOrWhiteSpace(args.Message?.Text))
        {
            return;
        }
        
        if (args.Message.Text.StartsWith(_telegramMenuStore.TelegramButtons.GoBackKeyboard))
        {
            var commandToGo = _commands.Single(menuCommand => menuCommand.Id == _telegramMenuStore.GoBackCommandId);

            _telegramMenuStore.StrategyData.ClearData();
            
            await commandToGo.ExecuteAsync(args.CancellationToken);
            
            return;
        }

        foreach (var menuCommand in _commands.Where(menuCommand => args.Message.Text.StartsWith(menuCommand.Id)))
        {
            await menuCommand.ExecuteAsync(args.CancellationToken);
                    
            return;
        }

        if (_telegramMenuStore.ButtonsForHandleIncomeData().All(x => x != _telegramMenuStore.LastCommandId))
        {
            return;
        }

        var command = _commands.Single(menuCommand => menuCommand.Id == _telegramMenuStore.LastCommandId);

        await command.HandleIncomeDataAsync(args.Message.Text, args.CancellationToken);
    }
    
    private async Task<ActionResult> InitTelegramMenuAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramService.OnTelegramBotUserChatUpdate -= TelegramServiceOnOnTelegramBotUserChatUpdate;
            
            var initTelegram = await _telegramService.InitAsync(cancellationToken: cancellationToken);
            if (initTelegram != ActionResult.Success)
            {
                _logger.LogError("Cannot init telegram bot. In {Method}", nameof(InitTelegramMenuAsync));

                return initTelegram;
            }

            var telegramTestConnectionResult = await _telegramService.TestBotConnectionAsync(cancellationToken: cancellationToken);
            if (telegramTestConnectionResult != ActionResult.Success)
            {
                _logger.LogError("Cannot connect to telegram bot. In {Method}", nameof(InitTelegramMenuAsync));
                
                return telegramTestConnectionResult;
            }
            
            _telegramService.OnTelegramBotUserChatUpdate += TelegramServiceOnOnTelegramBotUserChatUpdate;

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogWarning("{Message}. In {Method}",
                taskCanceledException.Message, nameof(InitTelegramMenuAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(InitTelegramMenuAsync));
            
            return ActionResult.SystemError;
        }
    }

    #endregion
}