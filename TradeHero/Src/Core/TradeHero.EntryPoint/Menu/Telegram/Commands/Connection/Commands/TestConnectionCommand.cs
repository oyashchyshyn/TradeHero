using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Base.Exceptions;
using TradeHero.Contracts.Client.Resolvers;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Connection.Commands;

internal class TestConnectionCommand : IMenuCommand
{
    private readonly ILogger<TestConnectionCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IBinanceResolver _binanceResolver;
    private readonly IConnectionRepository _connectionRepository;
    private readonly TelegramMenuStore _telegramMenuStore;

    public TestConnectionCommand(
        ILogger<TestConnectionCommand> logger,
        ITelegramService telegramService,
        IBinanceResolver binanceResolver,
        IConnectionRepository connectionRepository,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _binanceResolver = binanceResolver;
        _connectionRepository = connectionRepository;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.ConnectionsTest;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _telegramMenuStore.LastCommandId = Id;
            
        var connections = await _connectionRepository.GetConnectionsAsync();
        if (!connections.Any())
        {
            _logger.LogError("There is no connections. In {Method}", nameof(HandleCallbackDataAsync));
                
            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
                
            return;
        }
            
        var inlineKeyboardButtons = connections.Select(strategy => 
            new List<InlineKeyboardButton>
            {
                new(strategy.IsActive ? $"{strategy.Name} (Active)" : strategy.Name)
                {
                    CallbackData = strategy.Id.ToString()
                }
            }
        );

        await _telegramService.SendTextMessageToUserAsync(
            $"Here you can test connection to exchanger.{Environment.NewLine}" +
            "<b>Tip:</b> You can test any connection even when bot strategy is running.", 
            _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Connections),
            cancellationToken: cancellationToken
        );
            
        await _telegramService.SendTextMessageToUserAsync(
            "Select connection that you want to test:", 
            _telegramMenuStore.GetInlineKeyboard(inlineKeyboardButtons),
            cancellationToken: cancellationToken
        );
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

            var binanceClientForTest = _binanceResolver.GenerateBinanceClient(connection.ApiKey, connection.SecretKey);
            if (binanceClientForTest == null)
            {
                _logger.LogWarning("{Property} is null. In {Method}", 
                    nameof(binanceClientForTest), nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("Cannot create client for test connection.", cancellationToken);
                
                return;
            }

            var apiKeyPermissionsAsync =
                await binanceClientForTest.SpotApi.Account.GetAPIKeyPermissionsAsync(ct: cancellationToken);

            if (!apiKeyPermissionsAsync.Success)
            {
                _logger.LogWarning(new ThException(apiKeyPermissionsAsync.Error), "In {Method}", 
                    nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("Cannot connect to binance with current api/secret key.", cancellationToken);
                
                return;
            }

            if (!apiKeyPermissionsAsync.Data.EnableFutures)
            {
                _logger.LogWarning("Futures for this api key is not enabled. In {Method}", 
                    nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("Futures for this api key is not enabled.", cancellationToken);
                
                return;
            }
            
            if (!apiKeyPermissionsAsync.Data.EnableSpotAndMarginTrading)
            {
                _logger.LogWarning("Futures for this api key is not enabled. In {Method}", 
                    nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("Spot and margin for this api key is not enabled.", cancellationToken);
                
                return;
            }

            await SendMessageWithClearDataAsync("Connect successfully to exchanger.", cancellationToken);
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