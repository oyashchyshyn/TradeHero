using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Services;
using TradeHero.EntryPoint.Dictionary;
using TradeHero.EntryPoint.Menu.Telegram.Helpers;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Strategy.Commands;

internal class ShowStrategiesPropertiesCommand : IMenuCommand
{
    private readonly ILogger<ShowStrategiesPropertiesCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly EnumDictionary _enumDictionary;
    private readonly TelegramMenuStore _telegramMenuStore;

    public ShowStrategiesPropertiesCommand(
        ILogger<ShowStrategiesPropertiesCommand> logger,
        ITelegramService telegramService,
        EnumDictionary enumDictionary,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _enumDictionary = enumDictionary;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StrategiesProperties;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;

            var listStrategyInlineKeyboard = 
                from strategyType in Enum.GetValues<StrategyType>().OrderByDescending(x => x) 
                where strategyType != StrategyType.NoStrategy 
                select new List<InlineKeyboardButton>
                {
                    new(_enumDictionary.GetStrategyTypeUserFriendlyName(strategyType))
                    {
                        CallbackData = strategyType.ToString()
                    }
                };

            var listInstanceInlineKeyboard = 
                from instanceType in Enum.GetValues<InstanceType>().OrderByDescending(x => x) 
                where instanceType != InstanceType.NoInstance 
                select new List<InlineKeyboardButton>
                {
                    new(_enumDictionary.GetInstanceTypeUserFriendlyName(instanceType))
                    {
                        CallbackData = instanceType.ToString()
                    }
                };

            await _telegramService.SendTextMessageToUserAsync(
                "Please, choose one of options below in order to see it's properties:", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Available strategies:", 
                _telegramMenuStore.GetInlineKeyboard(listStrategyInlineKeyboard),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Available instances:", 
                _telegramMenuStore.GetInlineKeyboard(listInstanceInlineKeyboard),
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
            if (Enum.TryParse(callbackData, out StrategyType strategyType))
            {
                var message =
                    $"All properties for <b>{_enumDictionary.GetStrategyTypeUserFriendlyName(strategyType)}</b>:{Environment.NewLine}{Environment.NewLine}" +
                    $"{MessageGenerator.GenerateCreateStrategyTypeMessage(strategyType)}{Environment.NewLine}";

                await SendMessageWithClearDataAsync(message, cancellationToken);

                return;
            }

            if (Enum.TryParse(callbackData, out InstanceType instanceType))
            {
                var message = 
                    $"All properties for <b>{_enumDictionary.GetInstanceTypeUserFriendlyName(instanceType)}</b>:{Environment.NewLine}{Environment.NewLine}" +
                    $"{MessageGenerator.GenerateCreateInstanceTypeMessage(instanceType)}{Environment.NewLine}";

                await SendMessageWithClearDataAsync(message, cancellationToken);
                
                return;
            }
            
            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
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