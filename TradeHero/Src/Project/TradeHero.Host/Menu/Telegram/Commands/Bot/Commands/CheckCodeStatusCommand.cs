using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Services;
using TradeHero.Host.Menu.Telegram.Store;

namespace TradeHero.Host.Menu.Telegram.Commands.Bot.Commands;

internal class CheckCodeStatusCommand : ITelegramMenuCommand
{
    private readonly ILogger<CheckCodeStatusCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStoreService _storeService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public CheckCodeStatusCommand(
        ILogger<CheckCodeStatusCommand> logger,
        ITelegramService telegramService, 
        IStoreService storeService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _storeService = storeService;
        _telegramMenuStore = telegramMenuStore;
    }

    public string Id => _telegramMenuStore.TelegramButtons.CheckCodeStatus;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id; 
        
            var message = string.Format("Critical: {0}{1}Errors {2}{3}Warnings: {4}{5}",
                _storeService.Information.CriticalCount,
                Environment.NewLine,
                _storeService.Information.ErrorCount,
                Environment.NewLine,
                _storeService.Information.WarningCount,
                Environment.NewLine
            );

            await _telegramService.SendTextMessageToUserAsync(
                message, 
                _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
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