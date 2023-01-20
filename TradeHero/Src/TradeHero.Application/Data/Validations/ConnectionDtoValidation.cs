using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Types.Client;
using TradeHero.Core.Types.Client.Resolvers;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Repositories.Models;

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
                .MustAsync(CheckNameAsync);

            RuleFor(x => x)
                .MustAsync(CheckApiAndSecretKeysAsync);
        });
    }
    
    #region Private methods

    private async Task<bool> CheckNameAsync(ConnectionDto connectionDto, 
        string name, ValidationContext<ConnectionDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.Name)], "Cannot be empty."));
                
                return false;
            }
            
            switch (name.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(ConnectionDto.Name)], "Minimum length 3."));
                    return false;
                case > 40:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(ConnectionDto.Name)], "Maximum length 40."));
                    return false;
            }

            if (!await _strategyRepository.IsNameExistInDatabaseForCreate(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.Name)], $"Connection with name '{name}' already exist."));
            }
            
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(CheckNameAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(ConnectionDto.Name)]}", 
                "Validation failed."));
            
            return false;
        }
    }

    private async Task<bool> CheckApiAndSecretKeysAsync(ConnectionDto connectionDto, ConnectionDto connectionDtoFromRule, 
        ValidationContext<ConnectionDto> propertyContext, CancellationToken cancellationToken)
    {
        IThRestBinanceClient? restBinanceClient = null;
        
        try
        {
            if (string.IsNullOrWhiteSpace(connectionDto.ApiKey))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.ApiKey)], "Cannot be empty."));
                
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(connectionDto.SecretKey))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(ConnectionDto.SecretKey)], "Cannot be empty."));
                
                return false;
            }
            
            restBinanceClient = _binanceResolver.GenerateBinanceClient(connectionDtoFromRule.ApiKey,
                connectionDtoFromRule.SecretKey);
            if (restBinanceClient == null)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Cannot get client for this api/secret key combination."));

                return false;
            }
            
            var apiKeyPermissionsRequest = await restBinanceClient.SpotApi.Account.GetAPIKeyPermissionsAsync(ct: cancellationToken);
            if (!apiKeyPermissionsRequest.Success)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Cannot connect to exchanger by this api/secret key combination."));

                return false;
            }

            if (!apiKeyPermissionsRequest.Data.EnableFutures)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Futures trading must be enabled."));

                return false;
            }
            
            if (!apiKeyPermissionsRequest.Data.EnableSpotAndMarginTrading)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                    "Spot and Margin trading must be enabled."));

                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(CheckApiAndSecretKeysAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(ConnectionDto.ApiKey)]}/{_propertyNames[nameof(ConnectionDto.ApiKey)]}", 
                "Validation failed."));
            
            return false;
        }
        finally
        {
            restBinanceClient?.Dispose();
        }
    }

    #endregion
}