using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Menu.Commands;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Services;

namespace TradeHero.Application.Menu.Telegram.Commands.Connection.Commands;

internal class SetActiveConnectionCommand : ITelegramMenuCommand
{
    private readonly ILogger<SetActiveConnectionCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStoreService _storeService;
    private readonly IConnectionRepository _connectionRepository;
    private readonly TelegramMenuStore _telegramMenuStore;

    public SetActiveConnectionCommand(
        ILogger<SetActiveConnectionCommand> logger,
        ITelegramService telegramService,
        IStoreService storeService,
        IConnectionRepository connectionRepository,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _storeService = storeService;
        _connectionRepository = connectionRepository;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.ConnectionsSetActive;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
            
            var connections = await _connectionRepository.GetConnectionsAsync();
            if (!connections.Any())
            {
                _logger.LogError("There is no connections. In {Method}", nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
                
                return;
            }
            
            var inlineKeyboardButtons = connections.Select(connection => 
                new List<InlineKeyboardButton>
                {
                    new(connection.IsActive ? $"{connection.Name} (Active)" : connection.Name)
                    {
                        CallbackData = connection.Id.ToString()
                    }
                }
            );

            await _telegramService.SendTextMessageToUserAsync(
                $"Here you can change current active connection.{Environment.NewLine}{Environment.NewLine}" +
                $"<b>Tip:</b> You cannot cannot change active connection when it's in trading process.{Environment.NewLine}" +
                "If you want to change active connection firs of all you need to stop strategy.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Connections),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Select connection that you want to set active:", 
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

            var connection = await _connectionRepository.GetConnectionByIdAsync(Guid.Parse(callbackData));
            if (connection == null)
            {
                _logger.LogWarning("Connection with key {Key} does not exist. In {Method}", 
                    callbackData, nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
                
                return;
            }

            if (connection.IsActive)
            {
                await SendMessageWithClearDataAsync("Connection already is active.", cancellationToken);
                
                return;
            }

            if (_storeService.Bot.TradeLogicStatus == TradeLogicStatus.Running)
            {
                await SendMessageWithClearDataAsync(
                    $"You cannot change connection when bot in trading process.{Environment.NewLine}" +
                    "First of all stop strategy, then you will be able change active status.", 
                    cancellationToken
                );
                
                return;
            }
            
            var result = await _connectionRepository.SetActiveConnectionAsync(connection.Id);
            if (result)
            {
                await SendMessageWithClearDataAsync($"<b>{connection.Name}</b> set as active.", cancellationToken);
                
                return;
            }

            await SendMessageWithClearDataAsync($"Cannot set <b>{connection.Name}</b> as active.", cancellationToken);
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
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Connections),
            cancellationToken: cancellationToken
        );
    }

    #endregion
}