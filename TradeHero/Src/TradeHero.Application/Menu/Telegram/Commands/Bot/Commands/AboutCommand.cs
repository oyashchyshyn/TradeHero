using Microsoft.Extensions.Logging;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Services;

namespace TradeHero.Application.Menu.Telegram.Commands.Bot.Commands;

internal class AboutCommand : ITelegramMenuCommand
{
    private readonly ILogger<AboutCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IEnvironmentService _environmentService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public AboutCommand(
        ILogger<AboutCommand> logger,
        ITelegramService telegramService,
        IEnvironmentService environmentService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _environmentService = environmentService;
        _telegramMenuStore = telegramMenuStore;
    }

    public string Id => _telegramMenuStore.TelegramButtons.About;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;

            await _telegramService.SendTextMessageToUserAsync(
                $"Version: v{_environmentService.GetCurrentApplicationVersion().ToString(3)}", 
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
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
            cancellationToken: cancellationToken
        );
    }

    #endregion
}