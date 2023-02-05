using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Repositories;
using TradeHero.Core.Contracts.Services;

namespace TradeHero.Application.Menu.Telegram.Commands.Connection.Commands;

internal class DeleteConnectionCommand : ITelegramMenuCommand
{
    private readonly ILogger<DeleteConnectionCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IConnectionRepository _connectionRepository;
    private readonly TelegramMenuStore _telegramMenuStore;

    public DeleteConnectionCommand(
        ILogger<DeleteConnectionCommand> logger,
        ITelegramService telegramService,
        IConnectionRepository connectionRepository,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _connectionRepository = connectionRepository;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.ConnectionsDelete;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.PreviousCommandId = _telegramMenuStore.TelegramButtons.Connections;
            _telegramMenuStore.LastCommandId = Id;
            
            var connections = await _connectionRepository.GetConnectionsAsync();
            if (!connections.Any())
            {
                _logger.LogError("There is no connections. In {Method}", nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("There is no connections.", cancellationToken);
                
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
                $"Here you can delete connection.{Environment.NewLine}{Environment.NewLine}<b>Tip:</b> You cannot delete active connection.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Connections),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Select connection that you want to delete:", 
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

                await SendMessageWithClearDataAsync("Connection does not exist.", cancellationToken);
                
                return;
            }

            if (connection.IsActive)
            {
                _logger.LogWarning("Cannot delete active connection. In {Method}", 
                    nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync("Cannot delete active strategy.", cancellationToken);

                return;
            }
            
            var result = await _connectionRepository.DeleteConnectionAsync(connection.Id);
            if (result)
            {
                _logger.LogInformation("Cannot delete connection. In {Method}", 
                    nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync($"<b>{connection.Name}</b> deleted successfully.", cancellationToken);
                
                return;
            }

            await SendMessageWithClearDataAsync($"Cannot delete <b>{connection.Name}</b> connection.", cancellationToken);
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