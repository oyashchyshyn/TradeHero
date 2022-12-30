using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Connection.Commands;

internal class ShowConnectionsCommand : IMenuCommand
{
    private readonly ILogger<ShowConnectionsCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public ShowConnectionsCommand(
        ILogger<ShowConnectionsCommand> logger,
        ITelegramService telegramService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.ConnectionsShow;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            await _telegramService.SendTextMessageToUserAsync(
                "Choose option:", 
                _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
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