using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;
using TradeHero.EntryPoint.Menu.Telegram.Store;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Bot.Commands;

internal class PidorCommand : IMenuCommand
{
    private readonly ILogger<PidorCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public PidorCommand(
        ILogger<PidorCommand> logger,
        ITelegramService telegramService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.Pidor;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            await _telegramService.SendTextMessageToUserAsync(
                "Write down a question", 
                _telegramMenuStore.GetRemoveKeyboard(),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ExecuteAsync));

            await ErrorMessageAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }

    public async Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken)
    {
        try
        {
            var message = data.Contains("pidor") || data.Contains("gay") 
                ? "Lizai moi anusai, chmo" 
                : "Sorry but I do not understand :(";

            await _telegramService.SendTextMessageToUserAsync(
                message, 
                _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(HandleIncomeDataAsync));
            
            await ErrorMessageAsync("There was an error during process, please, try later.", cancellationToken);
        }
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