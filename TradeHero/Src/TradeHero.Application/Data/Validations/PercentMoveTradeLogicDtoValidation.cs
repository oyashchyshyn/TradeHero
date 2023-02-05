using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Data.Dtos.TradeLogic;
using TradeHero.Core.Contracts.Repositories;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;

namespace TradeHero.Application.Data.Validations;

internal class PercentMoveStrategyDtoValidation : AbstractValidator<PercentMoveTradeLogicDto>
{
    private readonly ILogger<PercentMoveStrategyDtoValidation> _logger;
    private readonly IStrategyRepository _strategyRepository;
    
    private ValidationRuleSet _validationRuleSet;
    private readonly Dictionary<string, string> _propertyNames = typeof(PercentMoveTradeLogicDto).GetPropertyNameAndJsonPropertyName();
    
    public PercentMoveStrategyDtoValidation(
        ILogger<PercentMoveStrategyDtoValidation> logger,
        IStrategyRepository strategyRepository
        )
    {
        _logger = logger;
        _strategyRepository = strategyRepository;
        
        RuleSet(ValidationRuleSet.Create.ToString(), () =>
        {
            _validationRuleSet = ValidationRuleSet.Create;

            GeneralRules();
            
            _validationRuleSet = ValidationRuleSet.Default;
        });
        
        RuleSet(ValidationRuleSet.Update.ToString(), () =>
        {
            _validationRuleSet = ValidationRuleSet.Update;

            GeneralRules();
            
            _validationRuleSet = ValidationRuleSet.Default;
        });
    }

    #region Private methods

    private void GeneralRules()
    {
        RuleFor(x => x.Name)
            .CustomAsync(ValidateNameAsync);
        
        RuleFor(x => x.PricePercentMove)
            .CustomAsync(ValidatePricePercentMoveAsync);
    }
    
    private async Task ValidateNameAsync(string name, ValidationContext<PercentMoveTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], "Cannot be empty."));
                
                return;
            }

            switch (name.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], "Minimum length 3."));
                    return;
                case > 40:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], "Maximum length 40."));
                    return;
            }

            var databaseCheckResult = false;
            switch (_validationRuleSet)
            {
                case ValidationRuleSet.Create:
                    databaseCheckResult = await _strategyRepository.IsNameExistInDatabaseForCreate(name);
                    break;
                case ValidationRuleSet.Update:
                    databaseCheckResult = await _strategyRepository.IsNameExistInDatabaseForUpdate(propertyContext.InstanceToValidate.Id, name);
                    break;
            }

            if (databaseCheckResult)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], $"Strategy with name '{name}' already exist."));
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateNameAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentMoveTradeLogicDto.Name)]}", 
                "Validation failed."));
        }
    }
    
    private Task ValidatePricePercentMoveAsync(decimal pricePercentMove, ValidationContext<PercentMoveTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (pricePercentMove)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.PricePercentMove)], 
                        "Cannot be lower then 0.01."));
                    return Task.CompletedTask;
                case > 1000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.PricePercentMove)], 
                        "Cannot be higher then 1000.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidatePricePercentMoveAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentMoveTradeLogicDto.PricePercentMove)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    #endregion
}