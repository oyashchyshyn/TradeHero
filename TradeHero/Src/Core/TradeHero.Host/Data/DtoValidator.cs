using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.EntryPoint.Data.Dtos.Instance;
using TradeHero.EntryPoint.Data.Dtos.TradeLogic;

namespace TradeHero.EntryPoint.Data;

internal class DtoValidator
{
    private readonly ILogger<DtoValidator> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public DtoValidator(
        ILogger<DtoValidator> logger, 
        IServiceProvider serviceProvider
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult?> GetValidationResultAsync<T>(T instance, ValidationRuleSet validationRuleSet = ValidationRuleSet.Default)
    {
        var validator = _serviceProvider.GetRequiredService<IValidator<T>>();
        return await validator.ValidateAsync(instance, 
            options => options.IncludeRuleSets(validationRuleSet.ToString()));
    }

    public async Task<ValidationResult?> GetValidationResultAsync(Type type, object instance, 
        ValidationRuleSet validationRuleSet = ValidationRuleSet.Default)
    {
        try
        {
            ValidationResult? validationResult = null;
        
            if (type == typeof(SpotClusterVolumeOptionsDto))
            {
                var validator = _serviceProvider.GetRequiredService<IValidator<SpotClusterVolumeOptionsDto>>();
                validationResult = await validator.ValidateAsync((SpotClusterVolumeOptionsDto)instance, 
                    options => options.IncludeRuleSets(validationRuleSet.ToString()));
            }
            else if (type == typeof(PercentLimitTradeLogicDto))
            {
                var validator = _serviceProvider.GetRequiredService<IValidator<PercentLimitTradeLogicDto>>();
                validationResult = await validator.ValidateAsync((PercentLimitTradeLogicDto)instance,
                    options => options.IncludeRuleSets(validationRuleSet.ToString()));
            }
            else if (type == typeof(PercentMoveTradeLogicDto))
            {
                var validator = _serviceProvider.GetRequiredService<IValidator<PercentMoveTradeLogicDto>>();
                validationResult = await validator.ValidateAsync((PercentMoveTradeLogicDto)instance,
                    options => options.IncludeRuleSets(validationRuleSet.ToString()));
            }

            return validationResult;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetValidationResultAsync));

            return null;
        }
    }
    
    public Type GetDtoTypeByStrategyType(TradeLogicType tradeLogicType)
    {
        return tradeLogicType switch
        {
            TradeLogicType.PercentLimit => typeof(PercentLimitTradeLogicDto),
            TradeLogicType.PercentMove => typeof(PercentMoveTradeLogicDto),
            TradeLogicType.NoTradeLogic => throw new ArgumentOutOfRangeException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public Type GetDtoTypeByInstanceType(InstanceType instanceType)
    {
        return instanceType switch
        {
            InstanceType.SpotClusterVolume => typeof(SpotClusterVolumeOptionsDto),
            InstanceType.NoInstance => throw new ArgumentOutOfRangeException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string GenerateValidationErrorMessage(List<ValidationFailure> validationFailures, Dictionary<string, string>? propertyNames = null)
    {
        var stringBuilder = new StringBuilder();
        
        stringBuilder.Append($"There was an error during data validation. Check errors:{Environment.NewLine}{Environment.NewLine}");

        if (propertyNames == null || !propertyNames.Any())
        {
            foreach (var validationFailure in validationFailures)
            {
                stringBuilder.Append(
                    $"<b>{validationFailure.PropertyName}</b> - {validationFailure.ErrorMessage}{Environment.NewLine}"
                );
            }   
        }
        else
        {
            foreach (var validationFailure in validationFailures)
            {
                stringBuilder.Append(
                    $"<b>{propertyNames[validationFailure.PropertyName]}</b> - {validationFailure.ErrorMessage}{Environment.NewLine}"
                );
            }
        }

        return stringBuilder.ToString();
    }
}