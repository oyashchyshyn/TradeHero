using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Contracts.Client;
using TradeHero.Core.Contracts.Repositories;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Models.Repositories;

namespace TradeHero.Application.Data.Validations;

internal class ConnectionDtoValidation : AbstractValidator<ConnectionDto>
{
    private readonly ILogger<ConnectionDtoValidation> _logger;
    private readonly IConnectionRepository _strategyRepository;
    private readonly IBinanceResolver _binanceResolver;

    private readonly Dictionary<string, string> _propertyNames = typeof(ConnectionDto).GetPropertyNameAndJsonPropertyName();
    
    public ConnectionDtoValidation(
        ILogger<ConnectionDtoValidation> logger,
        IConnectionRepository strategyRepository, 
        IBinanceResolver binanceResolver
        )
    {
        _logger = logger;
        _strategyRepository = strategyRepository;
        _binanceResolver = binanceResolver;

        RuleSet(ValidationRuleSet.Default.ToString(), () =>
        {
            RuleFor(x => x.Name)
                .CustomAsync(CheckNameAsync);

            RuleFor(x => x)
                .CustomAsync(CheckApiAndSecretKeysAsync);
        });
    }
    
    #region Private methods

    private async Task CheckNameAsync(string name, ValidationContext<ConnectionDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.Name)], "Cannot be empty."));
                
                return;
            }
            
            switch (name.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(ConnectionDto.Name)], "Minimum length 3."));
                    return;
                case > 40:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(ConnectionDto.Name)], "Maximum length 40."));
                    return;
            }

            if (!await _strategyRepository.IsNameExistInDatabaseForCreate(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.Name)], $"Connection with name '{name}' already exist."));
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(CheckNameAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(ConnectionDto.Name)]}", 
                "Validation failed."));
        }
    }

    private async Task CheckApiAndSecretKeysAsync(ConnectionDto connectionDto, ValidationContext<ConnectionDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        IThRestBinanceClient? restBinanceClient = null;
        
        try
        {
            if (string.IsNullOrWhiteSpace(connectionDto.ApiKey))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.ApiKey)], "Cannot be empty."));
                
                return;
            }
            
            if (string.IsNullOrWhiteSpace(connectionDto.SecretKey))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.SecretKey)], "Cannot be empty."));
                
                return;
            }
            
            restBinanceClient = _binanceResolver.GenerateBinanceClient(connectionDto.ApiKey,
                connectionDto.SecretKey);
            if (restBinanceClient == null)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Cannot get client for this api/secret key combination."));

                return;
            }
            
            var apiKeyPermissionsRequest = await restBinanceClient.SpotApi.Account.GetAPIKeyPermissionsAsync(ct: cancellationToken);
            if (!apiKeyPermissionsRequest.Success)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Cannot connect to exchanger by this api/secret key combination."));

                return;
            }

            if (!apiKeyPermissionsRequest.Data.EnableFutures)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Futures trading must be enabled."));

                return;
            }
            
            if (!apiKeyPermissionsRequest.Data.EnableSpotAndMarginTrading)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Spot and Margin trading must be enabled."));
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(CheckApiAndSecretKeysAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                "Validation failed."));
        }
        finally
        {
            restBinanceClient?.Dispose();
        }
    }

    #endregion
}