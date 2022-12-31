using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Extensions;
using TradeHero.EntryPoint.Data.Dtos.Instance;
using TradeHero.EntryPoint.Data.Dtos.Strategy;

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
        
            if (type == typeof(ClusterVolumeInstanceDto))
            {
                var validator = _serviceProvider.GetRequiredService<IValidator<ClusterVolumeInstanceDto>>();
                validationResult = await validator.ValidateAsync((ClusterVolumeInstanceDto)instance, 
                    options => options.IncludeRuleSets(validationRuleSet.ToString()));
            }
            else if (type == typeof(PercentLimitStrategyDto))
            {
                var validator = _serviceProvider.GetRequiredService<IValidator<PercentLimitStrategyDto>>();
                validationResult = await validator.ValidateAsync((PercentLimitStrategyDto)instance,
                    options => options.IncludeRuleSets(validationRuleSet.ToString()));
            }
            else if (type == typeof(PercentMoveStrategyDto))
            {
                var validator = _serviceProvider.GetRequiredService<IValidator<PercentMoveStrategyDto>>();
                validationResult = await validator.ValidateAsync((PercentMoveStrategyDto)instance,
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
    
    public Type GetDtoTypeByStrategyType(StrategyType strategyType)
    {
        return strategyType switch
        {
            StrategyType.PercentLimit => typeof(PercentLimitStrategyDto),
            StrategyType.PercentMove => typeof(PercentMoveStrategyDto),
            StrategyType.NoStrategy => throw new ArgumentOutOfRangeException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public Type GetDtoTypeByInstanceType(InstanceType instanceType)
    {
        return instanceType switch
        {
            InstanceType.SpotClusterVolume => typeof(ClusterVolumeInstanceDto),
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