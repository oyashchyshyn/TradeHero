using Microsoft.Extensions.Logging;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Services;

namespace TradeHero.Application.Menu.Telegram.Commands.Connection;

internal class ConnectionCommand : ITelegramMenuCommand
{
    private readonly ILogger<ConnectionCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public ConnectionCommand(
        ILogger<ConnectionCommand> logger,
        ITelegramService telegramService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.Connections;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            await _telegramService.SendTextMessageToUserAsync(
                "Choose option:", 
                _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Connections),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ExecuteAsync));

            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
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
    
    private async Task SendMessageWithClearDataAsync(string message, CancellationToken cancellationToken)
    {
        _telegramMenuStore.ClearData();

        await _telegramService.SendTextMessageToUserAsync(
            message,
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
            cancellationToken: cancellationToken
        );
    }

    #endregion
}