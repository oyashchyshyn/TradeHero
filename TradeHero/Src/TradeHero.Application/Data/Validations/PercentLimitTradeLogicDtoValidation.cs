using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Data.Dtos.TradeLogic;
using TradeHero.Core.Contracts.Repositories;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;

namespace TradeHero.Application.Data.Validations;

internal class PercentLimitStrategyDtoValidation : AbstractValidator<PercentLimitTradeLogicDto>
{
    private readonly ILogger<PercentLimitStrategyDtoValidation> _logger;
    private readonly IStrategyRepository _strategyRepository;
    
    private ValidationRuleSet _validationRuleSet;
    private readonly Dictionary<string, string> _propertyNames = typeof(PercentLimitTradeLogicDto).GetPropertyNameAndJsonPropertyName();
    
    public PercentLimitStrategyDtoValidation(
        ILogger<PercentLimitStrategyDtoValidation> logger,
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
        
        RuleFor(x => x.Leverage)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 1, 125, nameof(PercentLimitTradeLogicDto.Leverage)));

        RuleFor(x => x.MaximumPositions)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0, 1000, nameof(PercentLimitTradeLogicDto.MaximumPositions)));
        
        RuleFor(x => x.MaximumPositionsPerIteration)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0, 1000, nameof(PercentLimitTradeLogicDto.MaximumPositionsPerIteration)));

        RuleFor(x => x.AvailableDepositPercentForTrading)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0.00m, 100.00m, nameof(PercentLimitTradeLogicDto.AvailableDepositPercentForTrading)))
            .When(x => x.EnableOpenPositions);

        RuleFor(x => x.PercentFromDepositForOpen)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0.01m, 100.00m, nameof(PercentLimitTradeLogicDto.PercentFromDepositForOpen)))
            .When(x => x.EnableOpenPositions);
        
        RuleFor(x => x.MinQuoteVolumeForOpen)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0.00m, 100000000.00m, nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForOpen)))
            .When(x => x.EnableOpenPositions);
        
        RuleFor(x => x.CoefficientOfSellBuyVolumeForOpen)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0, 100.00m, nameof(PercentLimitTradeLogicDto.CoefficientOfSellBuyVolumeForOpen)))
            .When(x => x.EnableOpenPositions);
        
        RuleFor(x => x.CoefficientOfBidAsksForOpen)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0, 100.00m, nameof(PercentLimitTradeLogicDto.CoefficientOfBidAsksForOpen)))
            .When(x => x.EnableOpenPositions);

        RuleFor(x => x.AverageToRoe)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, -10000.00m, 0.00m, nameof(PercentLimitTradeLogicDto.AverageToRoe)))
            .When(x => x.EnableAveraging);

        RuleFor(x => x.AverageFromRoe)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, -10000.00m, 10000.00m, nameof(PercentLimitTradeLogicDto.AverageFromRoe)))
            .When(x => x.EnableAveraging);

        RuleFor(x => x.MinQuoteVolumeForAverage)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0.00m, 100000000.00m, nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForAverage)))
            .When(x => x.EnableAveraging);
        
        RuleFor(x => x.CoefficientOfSellBuyVolumeForAverage)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0, 100.00m, nameof(PercentLimitTradeLogicDto.CoefficientOfSellBuyVolumeForAverage)))
            .When(x => x.EnableOpenPositions);
        
        RuleFor(x => x.CoefficientOfBidAsksForAverage)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0, 100.00m, nameof(PercentLimitTradeLogicDto.CoefficientOfBidAsksForAverage)))
            .When(x => x.EnableOpenPositions);
        
        RuleFor(x => x.TrailingStopRoe)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, -10000.00m, 10000.00m, nameof(PercentLimitTradeLogicDto.TrailingStopRoe)))
            .When(x => x.EnableTrailingStops);

        RuleFor(x => x.CallbackRate)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0.1m, 5.0m, nameof(PercentLimitTradeLogicDto.CallbackRate)))
            .When(x => x.EnableTrailingStops);

        RuleFor(x => x.MarketStopSafePriceFromLastPricePercent)
            .CustomAsync(async (propertyValue, context, _) =>
            {
                if (propertyValue.HasValue)
                {
                    await ValidateRangeAsync(propertyValue.Value, context, 0.01m, 100.00m,
                        nameof(PercentLimitTradeLogicDto.MarketStopSafePriceFromLastPricePercent));
                }
            })
            .When(x => x.EnableTrailingStops && x.MarketStopSafePriceFromLastPricePercent.HasValue);
        
        RuleFor(x => x.MarketStopExitRoeActivation)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, -10000.00m, 10000.00m, nameof(PercentLimitTradeLogicDto.MarketStopExitRoeActivation)))
            .When(x => x.EnableMarketStopToExit);

        RuleFor(x => x.MarketStopExitPriceFromLastPricePercent)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0.00m, 100.00m, nameof(PercentLimitTradeLogicDto.MarketStopExitPriceFromLastPricePercent)))
            .When(x => x.EnableMarketStopToExit);
        
        RuleFor(x => x.MarketStopExitActivationFromAvailableBalancePercent)
            .CustomAsync(async (propertyValue, context, _) =>
            {
                if (propertyValue.HasValue)
                {
                    await ValidateRangeAsync(propertyValue.Value, context, 0.00m, 100.00m,
                        nameof(PercentLimitTradeLogicDto.MarketStopExitActivationFromAvailableBalancePercent));
                }
            })
            .When(x => x.EnableMarketStopToExit && x.MarketStopExitActivationFromAvailableBalancePercent.HasValue);

        RuleFor(x => x.MarketStopExitActivationAfterTime)
            .CustomAsync(ValidateMarketStopExitActivationAfterTimeAsync)
            .When(x => x.EnableMarketStopToExit && x.MarketStopExitActivationAfterTime.HasValue);

        RuleFor(x => x.StopLossPercentFromDeposit)
            .CustomAsync(async (propertyValue, context, _) => 
                await ValidateRangeAsync(propertyValue, context, 0.01m, 100.00m, nameof(PercentLimitTradeLogicDto.StopLossPercentFromDeposit)))
            .When(x => x.EnableMarketStopLoss);
    }

    private async Task ValidateNameAsync(string name, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], "Cannot be empty."));
                
                return;
            }

            switch (name.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], "Minimum length 3."));
                    return;
                case > 40:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], "Maximum length 40."));
                    return;
            }

            var databaseCheckResult = _validationRuleSet switch
            {
                ValidationRuleSet.Create => await _strategyRepository.IsNameExistInDatabaseForCreate(name),
                ValidationRuleSet.Update => await _strategyRepository.IsNameExistInDatabaseForUpdate(propertyContext.InstanceToValidate.Id, name),
                _ => false
            };

            if (databaseCheckResult)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], $"Strategy with name '{name}' already exist."));
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateNameAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.Name)]}", 
                "Validation failed."));
        }
    }

    private Task ValidateMarketStopExitActivationAfterTimeAsync(TimeSpan? marketStopExitActivationAfterTime, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (!marketStopExitActivationAfterTime.HasValue)
            {
                return Task.CompletedTask;
            }

            if (marketStopExitActivationAfterTime.Value < TimeSpan.Parse("00:00:01"))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationAfterTime)], 
                    "Cannot be lower then 00:00:01."));
                
                return Task.CompletedTask;
            }
            
            if (marketStopExitActivationAfterTime.Value > TimeSpan.Parse("24:00:00"))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationAfterTime)], 
                    "Cannot be higher then 24:00:00."));
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitActivationAfterTimeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationAfterTime)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }

    private Task ValidateRangeAsync(decimal valueToValidate, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        decimal lower, decimal higher, string propertyName)
    {
        try
        {
            if (valueToValidate < lower)
            {
                propertyContext.AddFailure(new ValidationFailure(_propertyNames[propertyName], 
                    $"Cannot be lower then {lower}."));
                return Task.CompletedTask;
            }
            
            if (valueToValidate > higher)
            {
                propertyContext.AddFailure(new ValidationFailure(_propertyNames[propertyName], 
                    $"Cannot be higher then {higher}."));
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateRangeAsync));
            
            propertyContext.AddFailure(new ValidationFailure($"{_propertyNames[propertyName]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateRangeAsync(int valueToValidate, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        int lower, int higher, string propertyName)
    {
        try
        {
            if (valueToValidate < lower)
            {
                propertyContext.AddFailure(new ValidationFailure(_propertyNames[propertyName], 
                    $"Cannot be lower then {lower}."));
                return Task.CompletedTask;
            }
            
            if (valueToValidate > higher)
            {
                propertyContext.AddFailure(new ValidationFailure(_propertyNames[propertyName], 
                    $"Cannot be higher then {higher}."));
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateRangeAsync));
            
            propertyContext.AddFailure(new ValidationFailure($"{_propertyNames[propertyName]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    #endregion
}