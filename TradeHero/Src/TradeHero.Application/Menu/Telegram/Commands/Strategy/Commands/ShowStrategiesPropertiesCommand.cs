﻿using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Application.Data.Dtos.Instance;
using TradeHero.Application.Data.Dtos.TradeLogic;
using TradeHero.Application.Dictionary;
using TradeHero.Application.Menu.Telegram.Store;
using TradeHero.Core.Contracts.Menu;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;

namespace TradeHero.Application.Menu.Telegram.Commands.Strategy.Commands;

internal class ShowStrategiesPropertiesCommand : ITelegramMenuCommand
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
            _telegramMenuStore.PreviousCommandId = _telegramMenuStore.TelegramButtons.Strategies;
            _telegramMenuStore.LastCommandId = Id;

            var listStrategyInlineKeyboard = 
                from strategyType in Enum.GetValues<TradeLogicType>().OrderByDescending(x => x) 
                where strategyType != TradeLogicType.NoTradeLogic 
                select new List<InlineKeyboardButton>
                {
                    new(_enumDictionary.GetTradeLogicTypeUserFriendlyName(strategyType))
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
            if (Enum.TryParse(callbackData, out TradeLogicType strategyType))
            {
                var propertyNamesWithDescription = strategyType switch
                {
                    TradeLogicType.PercentLimit => typeof(PercentLimitTradeLogicDto).GetJsonPropertyNameAndDescriptionFromType(),
                    TradeLogicType.PercentMove => typeof(PercentMoveTradeLogicDto).GetJsonPropertyNameAndDescriptionFromType(),
                    TradeLogicType.NoTradeLogic => throw new ArgumentOutOfRangeException(nameof(strategyType), strategyType, null),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategyType), strategyType, null)
                };

                var stringBuilder = new StringBuilder();
        
                foreach (var propertyNameWithDescription in propertyNamesWithDescription)
                {
                    stringBuilder.Append($"<b>{propertyNameWithDescription.Key}</b> - <i>{propertyNameWithDescription.Value}</i>{Environment.NewLine}");
                }

                var message =
                    $"All properties for <b>{_enumDictionary.GetTradeLogicTypeUserFriendlyName(strategyType)}</b>:{Environment.NewLine}{Environment.NewLine}" +
                    $"{stringBuilder}{Environment.NewLine}";

                await SendMessageWithClearDataAsync(message, cancellationToken);

                return;
            }

            if (Enum.TryParse(callbackData, out InstanceType instanceType))
            {
                var propertyNamesWithDescription = instanceType switch
                {
                    InstanceType.SpotClusterVolume => typeof(SpotClusterVolumeOptionsDto).GetJsonPropertyNameAndDescriptionFromType(),
                    InstanceType.NoInstance => throw new ArgumentOutOfRangeException(nameof(instanceType), instanceType, null),
                    _ => throw new ArgumentOutOfRangeException(nameof(instanceType), instanceType, null)
                };

                var stringBuilder = new StringBuilder();
        
                foreach (var propertyNameWithDescription in propertyNamesWithDescription)
                {
                    stringBuilder.Append($"<b>{propertyNameWithDescription.Key}</b> - <i>{propertyNameWithDescription.Value}</i>{Environment.NewLine}");
                }

                var message = 
                    $"All properties for <b>{_enumDictionary.GetInstanceTypeUserFriendlyName(instanceType)}</b>:{Environment.NewLine}{Environment.NewLine}" +
                    $"{stringBuilder}{Environment.NewLine}";

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