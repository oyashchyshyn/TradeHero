using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Menu.Data;
using TradeHero.Menu.Data.Dtos.Base;
using TradeHero.Menu.Dictionary;
using TradeHero.Menu.Menu.Telegram.Store;

namespace TradeHero.Menu.Menu.Telegram.Commands.Strategy.Commands;

internal class AddStrategyCommand : ITelegramMenuCommand
{
    private readonly ILogger<AddStrategyCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IJsonService _jsonService;
    private readonly DtoValidator _dtoValidator;
    private readonly EnumDictionary _enumDictionary;
    private readonly TelegramMenuStore _telegramMenuStore;

    public AddStrategyCommand(
        ILogger<AddStrategyCommand> logger,
        ITelegramService telegramService,
        IStrategyRepository strategyRepository,
        IJsonService jsonService,
        DtoValidator dtoValidator,
        EnumDictionary enumDictionary,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _strategyRepository = strategyRepository;
        _jsonService = jsonService;
        _dtoValidator = dtoValidator;
        _enumDictionary = enumDictionary;
        _telegramMenuStore = telegramMenuStore;
    }

    public string Id => _telegramMenuStore.TelegramButtons.StrategiesAdd;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
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

            await _telegramService.SendTextMessageToUserAsync(
                $"Here you can create strategies with instances. Please, be attentive during filling in properties.{Environment.NewLine}" +
                "Also, remember, that strategy name must be unique. Select strategy that you want to create.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Strategies:", 
                _telegramMenuStore.GetInlineKeyboard(listStrategyInlineKeyboard),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ExecuteAsync));
            
            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }

    public async Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken)
    {
        try
        {
            if (_telegramMenuStore.StrategyData.InstanceType != InstanceType.NoInstance)
            {
                var jsonExpandoObject = _jsonService.ConvertKeyValueStringDataToDictionary(data);
                if (jsonExpandoObject.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var jsonExpandoString = _jsonService.SerializeObject(jsonExpandoObject.Data);
                if (jsonExpandoString.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var instanceType = _dtoValidator.GetDtoTypeByInstanceType(_telegramMenuStore.StrategyData.InstanceType);
                
                var instanceObjectResult = _jsonService.Deserialize(jsonExpandoString.Data, instanceType);
                if (instanceObjectResult.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var validationResult = await _dtoValidator.GetValidationResultAsync(instanceType, instanceObjectResult.Data);
                if (validationResult == null)
                {
                    _logger.LogError("{Property} is null. In {Method}", 
                        nameof(validationResult), nameof(HandleIncomeDataAsync));
                    
                    await _telegramService.SendTextMessageToUserAsync(
                        "Error during data validation, please try again.",
                        cancellationToken: cancellationToken
                    );

                    return;
                }

                if (!validationResult.IsValid)
                {
                    _logger.LogError("There is an errors during validation. Errors: {Errors}. In {Method}", 
                        _jsonService.SerializeObject(validationResult.Errors).Data, nameof(HandleIncomeDataAsync));
                
                    await _telegramService.SendTextMessageToUserAsync(
                        _dtoValidator.GenerateValidationErrorMessage(validationResult.Errors, instanceType.GetPropertyNameAndJsonPropertyName()),
                        cancellationToken: cancellationToken
                    );
                    
                    await _telegramService.SendTextMessageToUserAsync(
                        "Please be attentive and try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var jsonData = _jsonService.SerializeObject(instanceObjectResult.Data, Formatting.None,
                    JsonSerializationSettings.IgnoreJsonPropertyName);
                if (instanceObjectResult.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }
                
                _telegramMenuStore.StrategyData.InstanceJson = jsonData.Data;
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Instance added.",
                    _telegramMenuStore.GetRemoveKeyboard(),
                    cancellationToken: cancellationToken
                );
                
                await SaveDataAsync(cancellationToken);
                
                return;
            }
            
            if (_telegramMenuStore.StrategyData.TradeLogicType != TradeLogicType.NoTradeLogic)
            {
                var jsonExpandoObject = _jsonService.ConvertKeyValueStringDataToDictionary(data);
                if (jsonExpandoObject.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var jsonExpandoString = _jsonService.SerializeObject(jsonExpandoObject.Data);
                if (jsonExpandoString.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var strategyType = _dtoValidator.GetDtoTypeByStrategyType(_telegramMenuStore.StrategyData.TradeLogicType);
                
                var strategyObjectResult = _jsonService.Deserialize(jsonExpandoString.Data, strategyType);
                if (strategyObjectResult.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var validationResult = await _dtoValidator.GetValidationResultAsync(strategyType, strategyObjectResult.Data, 
                    ValidationRuleSet.Create);
                if (validationResult == null)
                {
                    _logger.LogError("{Property} is null. In {Method}", 
                        nameof(validationResult), nameof(HandleIncomeDataAsync));
                    
                    await _telegramService.SendTextMessageToUserAsync(
                        "Error during data validation, please try again.",
                        cancellationToken: cancellationToken
                    );

                    return;
                }

                if (!validationResult.IsValid)
                {
                    _logger.LogError("There is an errors during validation. Errors: {Errors}. In {Method}", 
                        _jsonService.SerializeObject(validationResult.Errors).Data, nameof(HandleIncomeDataAsync));
                
                    await _telegramService.SendTextMessageToUserAsync(
                        _dtoValidator.GenerateValidationErrorMessage(validationResult.Errors, strategyType.GetPropertyNameAndJsonPropertyName()),
                        cancellationToken: cancellationToken
                    );
                    
                    await _telegramService.SendTextMessageToUserAsync(
                        "Please be attentive and try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                var jObjectResult = _jsonService.GetJObject(strategyObjectResult.Data, 
                    JsonSerializationSettings.IgnoreJsonPropertyName);

                if (jObjectResult.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                jObjectResult.Data.Remove(nameof(BaseStrategyDto.Name));

                var jsonData = _jsonService.SerializeObject(jObjectResult.Data, 
                    Formatting.None, JsonSerializationSettings.IgnoreJsonPropertyName);
                if (strategyObjectResult.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                _telegramMenuStore.StrategyData.StrategyName = ((BaseStrategyDto)strategyObjectResult.Data).Name;
                _telegramMenuStore.StrategyData.StrategyJson = jsonData.Data;

                var inlineKeyboardButtons = Enum.GetValues<InstanceType>()
                    .OrderByDescending(x => x)
                    .Select(instance => new List<InlineKeyboardButton>
                        {
                            new(_enumDictionary.GetInstanceTypeUserFriendlyName(instance))
                            {
                                CallbackData = instance.ToString()
                            }
                        }
                    );

                await _telegramService.SendTextMessageToUserAsync(
                    $"Strategy added.{Environment.NewLine}{Environment.NewLine}Do you want to add instance?",
                    _telegramMenuStore.GetInlineKeyboard(inlineKeyboardButtons),
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(HandleIncomeDataAsync));
            
            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }
    
    public async Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            if (Enum.TryParse(callbackData, out TradeLogicType strategyType))
            {
                _telegramMenuStore.StrategyData.TradeLogicType = strategyType;

                await _telegramService.SendTextMessageToUserAsync(
                    $"Example how to fill properties:{Environment.NewLine}{Environment.NewLine}" +
                    $"key:value{Environment.NewLine}key1:value1{Environment.NewLine}{Environment.NewLine}" +
                    "Please, fill in all properties with values in a proper way. Use only existing properties for current strategy.",
                    cancellationToken: cancellationToken
                );

                return;
            }

            if (Enum.TryParse(callbackData, out InstanceType instanceType))
            {
                if (instanceType == InstanceType.NoInstance)
                {
                    await SaveDataAsync(cancellationToken);
                
                    return;
                }
            
                _telegramMenuStore.StrategyData.InstanceType = instanceType;

                await _telegramService.SendTextMessageToUserAsync(
                    $"Example how to fill properties:{Environment.NewLine}{Environment.NewLine}" +
                    $"key:value{Environment.NewLine}key1:value1{Environment.NewLine}{Environment.NewLine}" +
                    "Please, fill in all properties with values in a proper way. Use only existing properties for current instance.",
                    cancellationToken: cancellationToken
                );
                
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

    private async Task SaveDataAsync(CancellationToken cancellationToken)
    {
        var savingResult = await _strategyRepository.AddStrategyAsync(new StrategyDto
        {
            Name = _telegramMenuStore.StrategyData.StrategyName,
            TradeLogicType = _telegramMenuStore.StrategyData.TradeLogicType,
            InstanceType = _telegramMenuStore.StrategyData.InstanceType,
            TradeLogicJson = _telegramMenuStore.StrategyData.StrategyJson,
            InstanceJson = _telegramMenuStore.StrategyData.InstanceJson,
            IsActive = false
        });

        if (!savingResult)
        {
            await SendMessageWithClearDataAsync("There was an error during saving data, please try again.", cancellationToken);
                        
            return;
        }

        await SendMessageWithClearDataAsync("Data saved is saved!", cancellationToken);
    }

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