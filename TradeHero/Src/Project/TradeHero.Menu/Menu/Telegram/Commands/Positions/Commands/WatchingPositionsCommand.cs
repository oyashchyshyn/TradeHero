using System.Text;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Menu.Menu.Telegram.Store;

namespace TradeHero.Menu.Menu.Telegram.Commands.Positions.Commands;

internal class WatchingPositionsCommand : ITelegramMenuCommand
{
    private readonly ILogger<WatchingPositionsCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStoreService _storeService;
    private readonly TelegramMenuStore _telegramMenuStore;
    
    public WatchingPositionsCommand(
        ILogger<WatchingPositionsCommand> logger,
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

    public string Id => _telegramMenuStore.TelegramButtons.WatchingPositions;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            var positions = _storeService.Bot.TradeLogic?.Store.Positions;

            if (positions == null || !positions.Any())
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "There is no opened positions.", 
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Positions),
                    cancellationToken: cancellationToken);
            
                return;
            }
        
            var stringBuilderList = new List<StringBuilder> { new() };
            var counter = 0;
            foreach (var position in positions)
            {
                var message = string.Format("S: {0}{1}S: {2}{3}L: x{4}{5}Q: {6}{7}E: {8}{9}{10}",
                    position.Name,
                    Environment.NewLine,
                    position.PositionSide,
                    Environment.NewLine,
                    position.Leverage,
                    Environment.NewLine,
                    position.TotalQuantity,
                    Environment.NewLine,
                    position.EntryPrice,
                    Environment.NewLine,
                    Environment.NewLine
                );

                if (stringBuilderList[counter].Length + message.Length < TelegramConstants.MaximumMessageLenght)
                {
                    stringBuilderList[counter].Append(message);
                
                    continue;
                }
            
                stringBuilderList.Add(new StringBuilder(message));
                counter++;
            }

            foreach (var stringBuilder in stringBuilderList)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    stringBuilder.ToString(), 
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Positions),
                    cancellationToken: cancellationToken
                );   
            }
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
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Positions),
            cancellationToken: cancellationToken
        );
    }

    #endregion
}