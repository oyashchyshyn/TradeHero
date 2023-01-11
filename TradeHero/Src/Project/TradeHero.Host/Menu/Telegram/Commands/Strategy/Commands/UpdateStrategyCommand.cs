using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Host.Data;
using TradeHero.Host.Data.Dtos.Base;
using TradeHero.Host.Dictionary;
using TradeHero.Host.Menu.Telegram.Store;

namespace TradeHero.Host.Menu.Telegram.Commands.Strategy.Commands;

internal class UpdateStrategyCommand : ITelegramMenuCommand
{
    private readonly ILogger<UpdateStrategyCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStore _store;
    private readonly IJsonService _jsonService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly DtoValidator _dtoValidator;
    private readonly EnumDictionary _enumDictionary;
    private readonly TelegramMenuStore _telegramMenuStore;

    public UpdateStrategyCommand(
        ILogger<UpdateStrategyCommand> logger,
        ITelegramService telegramService,
        IStore store,
        IJsonService jsonService,
        IStrategyRepository strategyRepository,
        DtoValidator dtoValidator,
        EnumDictionary enumDictionary,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _store = store;
        _jsonService = jsonService;
        _strategyRepository = strategyRepository;
        _dtoValidator = dtoValidator;
        _enumDictionary = enumDictionary;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StrategiesUpdate;

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
                $"Here you can update any strategy property. It's related to strategy or instance.{Environment.NewLine}" +
                "Also, if strategy does not have instance you able to add it.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Select strategy that you want to update:", 
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

    public async Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = await _strategyRepository.GetStrategyByIdAsync(Guid.Parse(_telegramMenuStore.StrategyData.StrategyId));
            if (strategy == null)
            {
                _logger.LogWarning("Strategy with name {Key} does not exist. In {Method}", 
                    _telegramMenuStore.StrategyData.StrategyId, nameof(HandleIncomeDataAsync));
                
                await SendMessageWithClearDataAsync("Selected strategy does not exist.", cancellationToken);
                
                return;
            }

            if (_telegramMenuStore.StrategyData.InstanceType != InstanceType.NoInstance)
            {
                var instanceJsonExpandoObject = _jsonService.ConvertKeyValueStringDataToDictionary(data);
                if (instanceJsonExpandoObject.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                        cancellationToken: cancellationToken
                    );
                
                    return;
                }

                var jsonExpandoString = _jsonService.SerializeObject(instanceJsonExpandoObject.Data);
                if (jsonExpandoString.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                        cancellationToken: cancellationToken
                    );
                
                    return;
                }

                var instanceType = _dtoValidator.GetDtoTypeByInstanceType(_telegramMenuStore.StrategyData.InstanceType);
                
                var tradeOptions = _jsonService.Deserialize(jsonExpandoString.Data, instanceType);
                if (tradeOptions.ActionResult != ActionResult.Success)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                        cancellationToken: cancellationToken
                    );
                
                    return;
                }

                var instanceValidationResult = await _dtoValidator.GetValidationResultAsync(instanceType, tradeOptions.Data);
                if (instanceValidationResult == null)
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        "Error during data validation, please try again.",
                        _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                        cancellationToken: cancellationToken
                    );

                    return;
                }
            
                if (instanceValidationResult.IsValid)
                {
                    strategy.InstanceJson = _jsonService.SerializeObject(tradeOptions.Data).Data;
                    strategy.InstanceType = _telegramMenuStore.StrategyData.InstanceType;
                
                    if (await UpdateStrategyAsync(strategy, cancellationToken))
                    {
                        return;
                    }
                
                    await SendMessageWithClearDataAsync("Error during data saving, please try again.", cancellationToken);
                
                    return;
                }

                await _telegramService.SendTextMessageToUserAsync(
                    _dtoValidator.GenerateValidationErrorMessage(instanceValidationResult.Errors, instanceType.GetPropertyNameAndJsonPropertyName()),
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.GoBackKeyboard),
                    cancellationToken: cancellationToken
                );
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Please be attentive and try again.",
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.GoBackKeyboard),
                    cancellationToken: cancellationToken
                );

                return;
            }
            
            JObject? jObject;
            Type? type;
            ValidationRuleSet validationRuleSet;
            
            switch (_telegramMenuStore.StrategyData.StrategyObjectToUpdate)
            {
                case StrategyObject.TradeLogic:
                    jObject = _jsonService.GetJObject(strategy.TradeLogicJson).Data;
                    jObject.Add(new JProperty(nameof(BaseStrategyDto.Name), strategy.Name));
                    validationRuleSet = ValidationRuleSet.Update;
                    type = _dtoValidator.GetDtoTypeByStrategyType(strategy.TradeLogicType);
                    break;
                case StrategyObject.Instance:
                    jObject = _jsonService.GetJObject(strategy.InstanceJson).Data;
                    validationRuleSet = ValidationRuleSet.Default;
                    type = _dtoValidator.GetDtoTypeByInstanceType(strategy.InstanceType);
                    break;
                case StrategyObject.None:
                default:
                    jObject = null;
                    validationRuleSet = ValidationRuleSet.Default;
                    type = null;
                    break;
            }

            if (jObject == null || type == null)
            {
                _logger.LogError("JObject is null: {JObjectIsNull}. Type is null: {TypeIsNull}. In {Method}",
                    jObject == null, type == null, nameof(HandleIncomeDataAsync));
                
                await _telegramService.SendTextMessageToUserAsync(
                    "Error during data validation, please try again.",
                    _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                    cancellationToken: cancellationToken
                );
                
                return;
            }
            
            var jsonExpandoObject = _jsonService.ConvertKeyValueStringDataToDictionary(data).Data;
            var objectPropertyNameAndJsonPropertyName = type.GetPropertyNameAndJsonPropertyName();
            foreach (var keyWithData in jsonExpandoObject)
            {
                var keyValuePair = objectPropertyNameAndJsonPropertyName.FirstOrDefault(x => x.Value == keyWithData.Key);
                if (string.IsNullOrWhiteSpace(keyValuePair.Key))
                {
                    await _telegramService.SendTextMessageToUserAsync(
                        $"Property name <b>{keyWithData.Key}</b> does not exist.{Environment.NewLine}" +
                        "Please be attentive and use only existing property names.",
                        _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                if (keyWithData.Value == null)
                {
                    _logger.LogError("{Key} has value with null. In {Method}", 
                        keyValuePair.Key, nameof(HandleIncomeDataAsync));
                    
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                if (jObject[keyValuePair.Key] == null)
                {
                    _logger.LogError("JObject with {Key} is null. In {Method}", 
                        keyValuePair.Key, nameof(HandleIncomeDataAsync));
                    
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        cancellationToken: cancellationToken
                    );
                    
                    return;
                }

                if (jObject[keyValuePair.Key]?.Type == JTokenType.Array)
                {
                    jObject[keyValuePair.Key] = new JArray(keyWithData.Value);   
                }
                else
                {
                    jObject[keyValuePair.Key] = new JValue(keyWithData.Value);  
                }
            }

            var objectWithData = _jsonService.Deserialize(jObject.ToString(), type, 
                JsonSerializationSettings.IgnoreJsonPropertyName);
            if (objectWithData.ActionResult != ActionResult.Success)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Inserted data is wrong, please try again.",
                    cancellationToken: cancellationToken
                );
                
                return;
            }

            ((BaseStrategyDto)objectWithData.Data).Id = strategy.Id;
            
            var validationResult = await _dtoValidator.GetValidationResultAsync(type, objectWithData.Data, 
                validationRuleSet);
            if (validationResult == null)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Inserted data is wrong, please try again.",
                    cancellationToken: cancellationToken
                );

                return;
            }
            
            if (!validationResult.IsValid)
            {
                _logger.LogError("There is an errors during validation. Errors: {Errors}. In {Method}", 
                    _jsonService.SerializeObject(validationResult.Errors).Data, nameof(HandleIncomeDataAsync));
                
                await _telegramService.SendTextMessageToUserAsync(
                    _dtoValidator.GenerateValidationErrorMessage(validationResult.Errors, type.GetPropertyNameAndJsonPropertyName()),
                    _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.GoBackKeyboard),
                    cancellationToken: cancellationToken
                );
                
                return;
            }

            var updatedDataData = _jsonService.SerializeObject(objectWithData.Data, Formatting.None,
                JsonSerializationSettings.IgnoreJsonPropertyName).Data;

            switch (_telegramMenuStore.StrategyData.StrategyObjectToUpdate)
            {
                case StrategyObject.TradeLogic:
                    strategy.TradeLogicJson = updatedDataData;
                    break;
                case StrategyObject.Instance:
                    strategy.InstanceJson = updatedDataData;
                    break;
                case StrategyObject.None:
                default:
                    _logger.LogError("{StrategyObject} cannot be update. In {Method}", 
                        _telegramMenuStore.StrategyData.StrategyObjectToUpdate, nameof(HandleIncomeDataAsync));
                    await _telegramService.SendTextMessageToUserAsync(
                        "Inserted data is wrong, please try again.",
                        _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                        cancellationToken: cancellationToken
                    );
                    break;
            }

            if (!await UpdateStrategyAsync(strategy, cancellationToken))
            {
                await SendMessageWithClearDataAsync("Error during data saving, please try again later.", cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(HandleIncomeDataAsync));

            await _telegramService.SendTextMessageToUserAsync(
                "Inserted data is wrong, please try again.",
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            if (Enum.TryParse(callbackData, out InstanceType instanceType))
            {
                await _telegramService.SendTextMessageToUserAsync(
                    $"Example how to fill properties:{Environment.NewLine}{Environment.NewLine}" +
                    $"key:value{Environment.NewLine}key1:value1{Environment.NewLine}{Environment.NewLine}" +
                    "Please, fill in all properties with values in a proper way. Use only existing properties for current instance.",
                    _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                    cancellationToken: cancellationToken
                );

                _telegramMenuStore.StrategyData.InstanceType = instanceType;
                
                return;
            }
            
            if (Enum.TryParse(callbackData, out StrategyObject strategyObject))
            {
                _telegramMenuStore.StrategyData.StrategyObjectToUpdate = strategyObject;
                
                switch (strategyObject)
                {
                    case StrategyObject.TradeLogic:
                        await _telegramService.SendTextMessageToUserAsync(
                            $"Example how to fill properties:{Environment.NewLine}{Environment.NewLine}" +
                            $"key:value{Environment.NewLine}key1:value1{Environment.NewLine}{Environment.NewLine}" +
                            "Please, fill in all properties with values in a proper way. Use only existing properties for current strategy.",
                            _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                            cancellationToken: cancellationToken
                        );

                        return;
                    case StrategyObject.Instance:
                    {
                        var strategy = await _strategyRepository.GetStrategyByIdAsync(Guid.Parse(_telegramMenuStore.StrategyData.StrategyId));

                        if (strategy?.InstanceType == InstanceType.NoInstance)
                        {
                            var instanceInlineKeyboardButtons = Enum.GetValues<InstanceType>()
                                .OrderByDescending(x => x);

                            var listInstanceInlineKeyboard = 
                                from instanceTypeButton in instanceInlineKeyboardButtons 
                                where instanceTypeButton != InstanceType.NoInstance 
                                select new List<InlineKeyboardButton>
                                {
                                    new(_enumDictionary.GetInstanceTypeUserFriendlyName(instanceTypeButton))
                                    {
                                        CallbackData = instanceTypeButton.ToString()
                                    }
                                };

                            await _telegramService.SendTextMessageToUserAsync(
                                "There is no instance for current strategy, do you want to add?",
                                _telegramMenuStore.GetInlineKeyboard(listInstanceInlineKeyboard),
                                cancellationToken: cancellationToken
                            );
                        
                            return;
                        }
                    
                        await _telegramService.SendTextMessageToUserAsync(
                            $"Example how to fill properties:{Environment.NewLine}{Environment.NewLine}" +
                            $"key:value{Environment.NewLine}key1:value1{Environment.NewLine}{Environment.NewLine}" +
                            "Please, fill in all properties with values in a proper way. Use only existing properties for current instance.",
                            _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                            cancellationToken: cancellationToken
                        );
                    
                        return;
                    }
                    case StrategyObject.None:
                    default:
                        _logger.LogWarning("Strategy with key {Key} does not exist. In {Method}", 
                            callbackData, nameof(HandleCallbackDataAsync));
                        await SendMessageWithClearDataAsync("Such strategy does not exist, please, try another one.", cancellationToken);
                        return;
                }
            }
            
            var strategyFromDatabase = await _strategyRepository.GetStrategyByIdAsync(Guid.Parse(callbackData));
            if (strategyFromDatabase == null)
            {
                _logger.LogWarning("Strategy with key {Key} does not exist. In {Method}", 
                    callbackData, nameof(HandleCallbackDataAsync));
                
                await SendMessageWithClearDataAsync("Such strategy does not exist, please, try another one..", cancellationToken);
                
                return;
            }
            
            _telegramMenuStore.StrategyData.StrategyId = callbackData;

            var listInlineKeyboard = 
                from strategyObjectValue in Enum.GetValues<StrategyObject>() 
                where strategyObjectValue != StrategyObject.None 
                select new List<InlineKeyboardButton>
                {
                    new(_enumDictionary.GetStrategyObjectUserFriendlyName(strategyObjectValue))
                    {
                        CallbackData = strategyObjectValue.ToString()
                    }
                };

            await _telegramService.SendTextMessageToUserAsync(
                "Select what you want to update:", 
                _telegramMenuStore.GetInlineKeyboard(listInlineKeyboard),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(HandleCallbackDataAsync));

            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }

    #region Private methods

    private async Task<bool> UpdateStrategyAsync(StrategyDto strategyDto, CancellationToken cancellationToken)
    {
        var updateResult = await _strategyRepository.UpdateStrategyAsync(strategyDto);
        if (!updateResult)
        {
            return false;
        }
        
        _telegramMenuStore.ClearData();
                
        await _telegramService.SendTextMessageToUserAsync(
            "Data saved successfully.",
            _telegramMenuStore.GetRemoveKeyboard(),
            cancellationToken: cancellationToken
        );

        if (_store.Bot is { TradeLogicStatus: TradeLogicStatus.Running, TradeLogic: { } } && strategyDto.IsActive)
        {
            await _telegramService.SendTextMessageToUserAsync(
                "Applying new data for running strategy...",
                _telegramMenuStore.GetRemoveKeyboard(),
                cancellationToken: cancellationToken
            );
                    
            await _store.Bot.TradeLogic.UpdateTradeSettingsAsync(strategyDto);
                    
            await _telegramService.SendTextMessageToUserAsync(
                "Data applied.",
                _telegramMenuStore.GetRemoveKeyboard(),
                cancellationToken: cancellationToken
            );
        }

        await _telegramService.SendTextMessageToUserAsync(
            "Choose action:",
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
            cancellationToken: cancellationToken
        );

        return true;
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