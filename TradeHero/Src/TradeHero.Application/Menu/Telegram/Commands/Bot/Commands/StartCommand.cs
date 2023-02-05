using Microsoft.Extensions.Logging;
using TradeHero.Application.Bot;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Services;

namespace TradeHero.Application.Menu.Telegram.Commands.Bot.Commands;

internal class StartCommand : ITelegramMenuCommand
{
    private readonly ILogger<StartCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly BotWorker _botWorker;
    private readonly TelegramMenuStore _telegramMenuStore;

    public StartCommand(
        ILogger<StartCommand> logger,
        ITelegramService telegramService, 
        BotWorker botWorker,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _botWorker = botWorker;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StartStrategy;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.PreviousCommandId = _telegramMenuStore.TelegramButtons.Bot;
            _telegramMenuStore.LastCommandId = Id;

            await _botWorker.StartTradeLogicAsync();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ExecuteAsync));

            await ErrorMessageAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }

    public Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    #region Private methods
    
    private async Task ErrorMessageAsync(string message, CancellationToken cancellationToken)
    {
        _telegramMenuStore.ClearData();
        
        await _telegramService.SendTextMessageToUserAsync(
            message,
            _telegramMenuStore.GetRemoveKeyboard(),
            cancellationToken: cancellationToken
        );
        
        await _telegramService.SendTextMessageToUserAsync(
            "Choose action:",
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
            cancellationToken: cancellationToken
        );
    }

    #endregion
}