using System.Dynamic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Types.Menu.Commands;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Services;
using TradeHero.Main.Data;
using TradeHero.Main.Menu.Telegram.Store;

namespace TradeHero.Main.Menu.Telegram.Commands.Strategy.Commands;

internal class ShowStrategiesCommand : ITelegramMenuCommand
{
    private readonly ILogger<ShowStrategiesCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IJsonService _jsonService;
    private readonly TelegramMenuStore _telegramMenuStore;
    private readonly DtoValidator _dtoValidator;

    public ShowStrategiesCommand(
        ILogger<ShowStrategiesCommand> logger,
        ITelegramService telegramService,
        IStrategyRepository strategyRepository,
        IJsonService jsonService,
        TelegramMenuStore telegramMenuStore, 
        DtoValidator dtoValidator
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _strategyRepository = strategyRepository;
        _jsonService = jsonService;
        _telegramMenuStore = telegramMenuStore;
        _dtoValidator = dtoValidator;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StrategiesShow;

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
                $"Here you can select existing strategy and see it's properties with strategy itself and instance.{Environment.NewLine}{Environment.NewLine}" +
                $"<b>Tip:</b> if you do not see instance properties it's mean that you did not create instance for strategy.{Environment.NewLine}" +
                "You can create instance in Update Strategy menu option.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
                cancellationToken: cancellationToken
            );
            
            await _telegramService.SendTextMessageToUserAsync(
                "Select strategy that you want to see settings:", 
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
            var strategy = await _strategyRepository.GetStrategyByIdAsync(Guid.Parse(callbackData));
            if (strategy == null)
            {
                _logger.LogWarning("Strategy with key {Key} does not exist. In {Method}", 
                    callbackData, nameof(HandleCallbackDataAsync));

                await SendMessageWithClearDataAsync("Strategy does not exist.", cancellationToken);
                
                return;
            }

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var strategyObject = _jsonService.Deserialize(strategy.TradeLogicJson, 
                _dtoValidator.GetDtoTypeByStrategyType(strategy.TradeLogicType), 
                JsonSerializationSettings.IgnoreJsonPropertyName);

            var strategyExpandoObject = GetExpandObject(strategyObject.Data);
            var strategyObjectSerialize = _jsonService.SerializeObject(strategyExpandoObject);
            var strategyJObject = _jsonService.GetJObject(strategyObjectSerialize.Data).Data;

            var strategyMessageList = new List<string>();
            foreach (var jProperty in strategyJObject.Properties())
            {
                if (jProperty.Name == "name")
                {
                    strategyMessageList.Add($"{jProperty.Name}: {strategy.Name}");
                    
                    continue;
                }
                
                if (jProperty.Value.Type == JTokenType.Null ||
                    (jProperty.Value.Type == JTokenType.String && string.IsNullOrEmpty(jProperty.Value.Value<string>())))
                {
                    continue;
                }

                strategyMessageList.Add($"{jProperty.Name}: {jProperty.Value}");
            }

            var message = $"<b>Trade options:</b>{Environment.NewLine}{Environment.NewLine}" +
                          $"{string.Join(Environment.NewLine, strategyMessageList)}";
            
            if (strategy.InstanceType != InstanceType.NoInstance)
            {
                var instanceObject = _jsonService.Deserialize(strategy.InstanceJson, 
                    _dtoValidator.GetDtoTypeByInstanceType(strategy.InstanceType),
                    JsonSerializationSettings.IgnoreJsonPropertyName);

                var instanceExpandoObject = GetExpandObject(instanceObject.Data);
                var instanceObjectSerialize = _jsonService.SerializeObject(instanceExpandoObject);
                var instanceJObject = _jsonService.GetJObject(instanceObjectSerialize.Data).Data;

                var instanceMessageList = new List<string>();
                foreach (var jProperty in instanceJObject.Properties())
                {
                    var jToken = instanceJObject.GetValue(jProperty.Name);

                    if (jToken?.Type == JTokenType.Null ||
                        (jToken?.Type == JTokenType.String && string.IsNullOrEmpty(jToken.Value<string>())))
                    {
                        continue;
                    }
                
                    instanceMessageList.Add($"{jProperty.Name}: {jToken}");
                }

                message += $"{Environment.NewLine}{Environment.NewLine}<b>Instances:</b>" +
                           $"{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, instanceMessageList)}";
            }

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
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Strategies),
            cancellationToken: cancellationToken
        );
    }

    private static ExpandoObject GetExpandObject(object obj)
    {
        var typeJsonProperties = obj.GetType().GetPropertyNameAndJsonPropertyName();
        var expando = new ExpandoObject();
        foreach (var property in obj.GetType().GetProperties())
        {
            var value = property.GetValue(obj);

            if (value is List<string> listValues)
            {
                value = string.Join(", ", listValues);
            }

            if (value is bool)
            {
                value = value.ToString()?.ToLower(); 
            }

            if (!typeJsonProperties.ContainsKey(property.Name))
            {
                continue;
            }
            
            expando.TryAdd(typeJsonProperties[property.Name], value);
        }

        return expando;
    }
    
    #endregion
}