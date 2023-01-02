using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Extensions;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Connection.Commands;

internal class ShowConnectionsCommand : IMenuCommand
{
    private readonly ILogger<ShowConnectionsCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConnectionRepository _connectionRepository;
    private readonly TelegramMenuStore _telegramMenuStore;

    public ShowConnectionsCommand(
        ILogger<ShowConnectionsCommand> logger,
        ITelegramService telegramService,
        IDateTimeService dateTimeService,
        IConnectionRepository connectionRepository,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _dateTimeService = dateTimeService;
        _connectionRepository = connectionRepository;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.ConnectionsShow;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
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
                "Here you can select connection and see it's properties", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Connections),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Select connection that you want to see settings:", 
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
            var connection = await _connectionRepository.GetConnectionByIdAsync(Guid.Parse(callbackData));
            if (connection == null)
            {
                _logger.LogWarning("Connection with key {Key} does not exist. In {Method}", 
                    callbackData, nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("Connection does not exist.", cancellationToken);
                
                return;
            }

            var localCreationDateTime = _dateTimeService.ConvertToLocalTime(connection.CreationDateTime);
            
            var message = $"{nameof(ConnectionDto.Name).LowercaseFirstLetter()}: {connection.Name}{Environment.NewLine}" +
                          $"{nameof(ConnectionDto.ApiKey).LowercaseFirstLetter()}: {connection.ApiKey}{Environment.NewLine}" +
                          $"{nameof(ConnectionDto.SecretKey).LowercaseFirstLetter()}: {connection.SecretKey}{Environment.NewLine}" +
                          $"{nameof(ConnectionDto.CreationDateTime).LowercaseFirstLetter()}: {localCreationDateTime:dd.MM.yyyy hh:mm:ss}";
            
            await SendMessageWithClearDataAsync(message, cancellationToken);
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