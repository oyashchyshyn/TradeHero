using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Services;
using TradeHero.Host.Menu.Telegram.Store;

namespace TradeHero.Host.Menu.Telegram.Commands.Bot;

internal class BotCommand : ITelegramMenuCommand
{
    private readonly ILogger<BotCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public BotCommand(
        ILogger<BotCommand> logger,
        ITelegramService telegramService, 
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.Bot;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            await _telegramService.SendTextMessageToUserAsync(
                "Choose option:", 
                _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
                cancellationToken: cancellationToken
            );
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