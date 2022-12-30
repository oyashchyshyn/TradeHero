using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.EntryPoint.Data;
using TradeHero.EntryPoint.Menu.Telegram.Helpers;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Connection.Commands;

internal class AddConnectionCommand : IMenuCommand
{
    private readonly ILogger<AddConnectionCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IJsonService _jsonService;
    private readonly IConnectionRepository _connectionRepository;
    private readonly DtoValidator _dtoValidator;
    private readonly TelegramMenuStore _telegramMenuStore;

    public AddConnectionCommand(
        ILogger<AddConnectionCommand> logger,
        ITelegramService telegramService,
        IJsonService jsonService,
        IConnectionRepository connectionRepository,
        DtoValidator dtoValidator,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _jsonService = jsonService;
        _connectionRepository = connectionRepository;
        _dtoValidator = dtoValidator;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.ConnectionsAdd;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            await _telegramService.SendTextMessageToUserAsync(
                $"Here you can create your connection to exchanger. Please, be attentive during filling in properties.{Environment.NewLine}" +
                $"<b>Example how to fill properties for connection:</b>{Environment.NewLine}{Environment.NewLine}" +
                $"name:<here name of connection>{Environment.NewLine}apiKey:<your api key>{Environment.NewLine}secretKey:<your secret key>" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "Please, fill in all properties with values in a proper way. Use only existing properties for current strategy.", 
                _telegramMenuStore.GetGoBackKeyboard(_telegramMenuStore.TelegramButtons.Connections),
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
            var jsonExpandoObject = _jsonService.ConvertKeyValueStringDataToDictionary(
                data, JsonKeyTransformation.ToCapitaliseCase);
            if (jsonExpandoObject.ActionResult != ActionResult.Success)
            {
                await _telegramService.SendTextMessageToUserAsync(
                    "Inserted data is wrong, please try again.",
                    cancellationToken: cancellationToken
                );
                    
                return;
            }

            var jObject = _jsonService.GetJObject(jsonExpandoObject.Data);
            var connectionDto = jObject.Data.ToObject<ConnectionDto>();
            if (connectionDto == null)
            {
                _logger.LogError("{Property} is null. In {Method}", 
                    nameof(connectionDto), nameof(HandleIncomeDataAsync));
                    
                await _telegramService.SendTextMessageToUserAsync(
                    "Error during data validation, please try again.",
                    cancellationToken: cancellationToken
                );

                return;
            }
            
            var validationResult = await _dtoValidator.GetValidationResultAsync(connectionDto, ValidationRuleSet.Create);
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
                    MessageGenerator.GenerateValidationErrorMessage(validationResult.Errors),
                    cancellationToken: cancellationToken
                );
                    
                await _telegramService.SendTextMessageToUserAsync(
                    "Please be attentive and try again.",
                    cancellationToken: cancellationToken
                );
                    
                return;
            }
            
            var savingResult = await _connectionRepository.AddConnectionAsync(connectionDto);
            if (!savingResult)
            {
                await SendMessageWithClearDataAsync("There was an error during saving data, please try again.", cancellationToken);
                        
                return;
            }

            await SendMessageWithClearDataAsync("Data saved is saved!", cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ExecuteAsync));

            await SendMessageWithClearDataAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }

    public Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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