using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Core.Types.Client.Resolvers;
using TradeHero.Core.Types.Menu.Commands;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Repositories.Models;
using TradeHero.Core.Types.Services;
using TradeHero.Main.Data;
using TradeHero.Main.Menu.Telegram.Store;

namespace TradeHero.Main.Menu.Telegram.Commands.Connection.Commands;

internal class AddConnectionCommand : ITelegramMenuCommand
{
    private readonly ILogger<AddConnectionCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IJsonService _jsonService;
    private readonly IBinanceResolver _binanceResolver;
    private readonly IConnectionRepository _connectionRepository;
    private readonly DtoValidator _dtoValidator;
    private readonly TelegramMenuStore _telegramMenuStore;

    public AddConnectionCommand(
        ILogger<AddConnectionCommand> logger,
        ITelegramService telegramService,
        IJsonService jsonService,
        IBinanceResolver binanceResolver,
        IConnectionRepository connectionRepository,
        DtoValidator dtoValidator,
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _jsonService = jsonService;
        _binanceResolver = binanceResolver;
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
                $"name: <here name of connection>{Environment.NewLine}apiKey: <your api key>{Environment.NewLine}secretKey: <your secret key>" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "Please, fill in all properties with values in a proper way. Use only existing properties.", 
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
                    _dtoValidator.GenerateValidationErrorMessage(validationResult.Errors),
                    cancellationToken: cancellationToken
                );
                    
                await _telegramService.SendTextMessageToUserAsync(
                    "Please be attentive and try again.",
                    cancellationToken: cancellationToken
                );
                    
                return;
            }
            
            var binanceClient = _binanceResolver.GenerateBinanceClient(
                connectionDto.ApiKey,
                connectionDto.SecretKey
            );

            if (binanceClient == null)
            {
                _logger.LogError("{Property} is null. In {Method}", 
                    nameof(binanceClient), nameof(HandleIncomeDataAsync));
                    
                await _telegramService.SendTextMessageToUserAsync(
                    "Error during creating client, please try again.",
                    cancellationToken: cancellationToken
                );

                return;
            }

            var apiKeyPermissionsRequest = await binanceClient.SpotApi.Account.GetAPIKeyPermissionsAsync(
                ct: cancellationToken);
            
            if (!apiKeyPermissionsRequest.Success)
            {
                _logger.LogError(new ThException(apiKeyPermissionsRequest.Error), "In {Method}", 
                    nameof(HandleIncomeDataAsync));
                    
                await _telegramService.SendTextMessageToUserAsync(
                    "Error during making request to exchanger, please try again.",
                    cancellationToken: cancellationToken
                );

                return;
            }

            connectionDto.CreationDateTime = apiKeyPermissionsRequest.Data.CreateTime;
            
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