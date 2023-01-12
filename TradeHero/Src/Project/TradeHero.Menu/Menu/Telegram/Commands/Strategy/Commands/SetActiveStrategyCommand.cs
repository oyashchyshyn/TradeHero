using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Menu.Menu.Telegram.Store;

namespace TradeHero.Menu.Menu.Telegram.Commands.Strategy.Commands;

internal class SetActiveStrategyCommand : ITelegramMenuCommand
{
    private readonly ILogger<SetActiveStrategyCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IStoreService _storeService;
    private readonly TelegramMenuStore _telegramMenuStore;

    public SetActiveStrategyCommand(
        ILogger<SetActiveStrategyCommand> logger,
        ITelegramService telegramService,
        IStrategyRepository strategyRepository,
        IStoreService storeService,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _strategyRepository = strategyRepository;
        _storeService = storeService;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StrategiesSetActive;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
            
            var strategies = await _strategyRepository.GetStrategiesAsync();
            if (!strategies.Any())
            {
                _logger.LogError("There is no strategies. In {Method}", nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
                
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
                $"Here you can change current active strategy.{Environment.NewLine}{Environment.NewLine}" +
                $"<b>Tip:</b> You cannot cannot change active strategy when it's in trading process.{Environment.NewLine}" +
                "If you want to change active strategy firs of all you need to stop strategy.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Select strategy that you want to set active:", 
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
                
                await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
                
                return;
            }

            if (strategy.IsActive)
            {
                await SendMessageWithClearDataAsync("Strategy already is active.", cancellationToken);
                
                return;
            }

            if (_storeService.Bot.TradeLogicStatus == TradeLogicStatus.Running)
            {
                await SendMessageWithClearDataAsync(
                    $"You cannot change strategy when bot in trading process.{Environment.NewLine}" +
                    "First of all stop strategy, then you will be able change active status.", 
                    cancellationToken
                );
                
                return;
            }
            
            var result = await _strategyRepository.SetActiveStrategyAsync(strategy.Id);
            if (result)
            {
                await SendMessageWithClearDataAsync($"<b>{strategy.Name}</b> set as active.", cancellationToken);
                
                return;
            }

            await SendMessageWithClearDataAsync($"Cannot set <b>{strategy.Name}</b> as active.", cancellationToken);
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