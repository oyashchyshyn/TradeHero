using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Store;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Bot.Commands;

internal class StopStrategyCommand : IMenuCommand
{
    private readonly ILogger<StopStrategyCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStore _store;
    private readonly TelegramMenuStore _telegramMenuStore;

    public StopStrategyCommand(
        ILogger<StopStrategyCommand> logger,
        ITelegramService telegramService, 
        IStore store, 
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _store = store;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StopStrategy;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            if (_store.Bot.TradeLogic == null)
            {
                await ErrorMessageAsync("Cannot stop strategy because it does not running.", cancellationToken);

                return;
            }
        
            await _telegramService.SendTextMessageToUserAsync(
                "Stopping...", 
                _telegramMenuStore.GetRemoveKeyboard(),
                cancellationToken: cancellationToken
            );

            var stopResult = await _store.Bot.TradeLogic.FinishAsync(true);
            if (stopResult != ActionResult.Success)
            {
                await ErrorMessageAsync("Error during stopping strategy.", cancellationToken);
                
                return;
            }
            
            _store.Bot.SetTradeLogic(null, TradeLogicStatus.Idle);

            await _telegramService.SendTextMessageToUserAsync(
                "Strategy stopped.", 
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