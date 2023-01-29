using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Menu;
using TradeHero.Core.Types.Menu.Commands;
using TradeHero.Core.Types.Services;
using TradeHero.Core.Types.Services.Models.Telegram;

namespace TradeHero.Application.Menu.Telegram;

internal class TelegramMenu : IMenuService
{
    private readonly ILogger<TelegramMenu> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IJsonService _jsonService;

    private readonly TelegramMenuStore _telegramMenuStore;
    
    private readonly List<ITelegramMenuCommand> _commands = new();

    public MenuType MenuType => MenuType.Telegram;
    
    public TelegramMenu(
        ILogger<TelegramMenu> logger,
        IServiceProvider serviceProvider,
        ITelegramService telegramService,
        IJsonService jsonService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _jsonService = jsonService;
        _telegramMenuStore = telegramMenuStore;

        _commands.AddRange(serviceProvider.GetServices<ITelegramMenuCommand>());
    }

    public async Task<ActionResult> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _telegramService.OnTelegramBotUserChatUpdate -= TelegramServiceOnOnTelegramBotUserChatUpdate;
            
            var initTelegram = await _telegramService.InitAsync(cancellationToken: cancellationToken);
            if (initTelegram != ActionResult.Success)
            {
                _logger.LogError("Cannot init telegram bot. In {Method}", nameof(InitAsync));

                return initTelegram;
            }

            var telegramTestConnectionResult = await _telegramService.TestBotConnectionAsync(cancellationToken: cancellationToken);
            if (telegramTestConnectionResult != ActionResult.Success)
            {
                _logger.LogError("Cannot connect to telegram bot. In {Method}", nameof(InitAsync));
                
                return telegramTestConnectionResult;
            }
            
            _telegramService.OnTelegramBotUserChatUpdate += TelegramServiceOnOnTelegramBotUserChatUpdate;

            return ActionResult.Success;
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


    public async Task<ActionResult> FinishAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _telegramService.OnTelegramBotUserChatUpdate -= TelegramServiceOnOnTelegramBotUserChatUpdate;
            _telegramMenuStore.LastCommandId = string.Empty;
            _telegramMenuStore.ClearData();
            
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
    
    public async Task<ActionResult> SendMessageAsync(string message, bool isNeedToShowMenu, CancellationToken cancellationToken = default)
    {
        try
        {
            if (isNeedToShowMenu)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    message, 
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.MainMenu),
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await _telegramService.SendTextMessageToUserAsync(
                    message, 
                    _telegramMenuStore.GetRemoveKeyboard(),
                    cancellationToken: cancellationToken
                );
            }

            return ActionResult.Success;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogInformation("{Message}. In {Method}",
                taskCanceledException.Message, nameof(InitAsync));
            
            return ActionResult.CancellationTokenRequested;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(InitAsync));
            
            return ActionResult.SystemError;
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

    #endregion
}