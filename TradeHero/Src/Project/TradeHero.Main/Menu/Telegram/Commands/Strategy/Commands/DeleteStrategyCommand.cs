using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Main.Menu.Telegram.Store;

namespace TradeHero.Main.Menu.Telegram.Commands.Strategy.Commands;

internal class DeleteStrategyCommand : ITelegramMenuCommand
{
    private readonly ILogger<DeleteStrategyCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly TelegramMenuStore _telegramMenuStore;

    public DeleteStrategyCommand(
        ILogger<DeleteStrategyCommand> logger,
        ITelegramService telegramService,
        IStrategyRepository strategyRepository,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _strategyRepository = strategyRepository;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StrategiesDelete;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
            
            var strategies = await _strategyRepository.GetStrategiesAsync();
            if (!strategies.Any())
            {
                _logger.LogError("There is no strategies. In {Method}", nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("There is no strategies.", cancellationToken);
                
                return;
            }
            
            var inlineKeyboardButtons = strategies.Select(strategy => 
                new List<InlineKeyboardButton>
                {
                    new(strategy.IsActive ? $"{strategy.Name} (Active)" : strategy.Name)
                    {
                        CallbackData = strategy.Id.ToString()
                    }
                }
            );

            await _telegramService.SendTextMessageToUserAsync(
                $"Here you can delete strategy.{Environment.NewLine}{Environment.NewLine}<b>Tip:</b> You cannot delete active strategy.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Select strategy that you want to delete:", 
                _telegramMenuStore.GetInlineKeyboard(inlineKeyboardButtons),
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
    
    public async Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(callbackData))
            {
                _logger.LogWarning("{Property} is null or empty. In {Method}", 
                    nameof(callbackData), nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
                
                return;
            }

            var strategy = await _strategyRepository.GetStrategyByIdAsync(Guid.Parse(callbackData));
            if (strategy == null)
            {
                _logger.LogWarning("Strategy with key {Key} does not exist. In {Method}", 
                    callbackData, nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("Strategy does not exist.", cancellationToken);
                
                return;
            }

            if (strategy.IsActive)
            {
                _logger.LogWarning("Cannot delete active strategy. In {Method}", 
                    nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync("Cannot delete active strategy.", cancellationToken);

                return;
            }
            
            var result = await _strategyRepository.DeleteStrategyAsync(strategy.Id);
            if (result)
            {
                _logger.LogInformation("Cannot delete strategy. In {Method}", 
                    nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync($"<b>{strategy.Name}</b> deleted successfully.", cancellationToken);
                
                return;
            }

            await SendMessageWithClearDataAsync($"Cannot delete <b>{strategy.Name}</b> strategy.", cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(HandleCallbackDataAsync));

            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
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