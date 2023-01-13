using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Repositories;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Main.Data.Dtos.TradeLogic;

namespace TradeHero.Main.Data.Validations;

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
            .MustAsync(ValidateNameAsync);
        
        RuleFor(x => x.PricePercentMove)
            .MustAsync(ValidatePricePercentMoveAsync);
    }
    
    private async Task<bool> ValidateNameAsync(PercentMoveTradeLogicDto percentMoveTradeLogicDto, 
        string name, ValidationContext<PercentMoveTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], "Cannot be empty."));
                
                return false;
            }

            switch (name.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], "Minimum length 3."));
                    return false;
                case > 40:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], "Maximum length 40."));
                    return false;
            }

            var databaseCheckResult = false;
            switch (_validationRuleSet)
            {
                case ValidationRuleSet.Create:
                    databaseCheckResult = await _strategyRepository.IsNameExistInDatabaseForCreate(name);
                    break;
                case ValidationRuleSet.Update:
                    databaseCheckResult = await _strategyRepository.IsNameExistInDatabaseForUpdate(percentMoveTradeLogicDto.Id, name);
                    break;
            }

            if (databaseCheckResult)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentMoveTradeLogicDto.Name)], $"Strategy with name '{name}' already exist."));

                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateNameAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentMoveTradeLogicDto.Name)]}", 
                "Validation failed."));
            
            return false;
        }
    }
    
    private Task<bool> ValidatePricePercentMoveAsync(PercentMoveTradeLogicDto percentMoveTradeLogicDto, 
        decimal pricePercentMove, ValidationContext<PercentMoveTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (pricePercentMove)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.PricePercentMove)], 
                        "Cannot be lower then 0.01."));
                    return Task.FromResult(false);
                case > 1000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentMoveTradeLogicDto.PricePercentMove)], 
                        "Cannot be higher then 1000.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidatePricePercentMoveAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentMoveTradeLogicDto.PricePercentMove)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    #endregion
}