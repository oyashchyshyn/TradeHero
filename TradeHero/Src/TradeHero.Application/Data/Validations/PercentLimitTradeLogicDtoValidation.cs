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
            .CustomAsync(ValidateLeverageAsync);
        
        RuleFor(x => x.MaximumPositions)
            .CustomAsync(ValidateMaximumPositionsAsync);
        
        RuleFor(x => x.MaximumPositionsPerIteration)
            .CustomAsync(ValidateMaximumPositionsPerIterationAsync);
        
        RuleFor(x => x.AvailableDepositPercentForTrading)
            .CustomAsync(ValidateAvailableDepositPercentForTradingAsync);
        
        RuleFor(x => x.PercentFromDepositForOpen)
            .CustomAsync(ValidatePercentFromDepositForOpenAsync)
            .When(x => x.EnableOpenPositions);

        RuleFor(x => x.MinQuoteVolumeForOpen)
            .CustomAsync(ValidateMinQuoteVolumeForOpenAsync)
            .When(x => x.EnableOpenPositions);

        RuleFor(x => x.AverageToRoe)
            .CustomAsync(ValidateAverageToRoeAsync)
            .When(x => x.EnableAveraging);
        
        RuleFor(x => x.AverageFromRoe)
            .CustomAsync(ValidateAverageFromRoeAsync)
            .When(x => x.EnableAveraging);

        RuleFor(x => x.MinQuoteVolumeForAverage)
            .CustomAsync(ValidateMinQuoteVolumeForAverageAsync)
            .When(x => x.EnableAveraging);
        
        RuleFor(x => x.TrailingStopRoe)
            .CustomAsync(ValidateTrailingStopRoeAsync)
            .When(x => x.EnableTrailingStops);
        
        RuleFor(x => x.CallbackRate)
            .CustomAsync(ValidateCallbackRateAsync)
            .When(x => x.EnableTrailingStops);

        RuleFor(x => x.MarketStopSafePriceFromLastPricePercent)
            .CustomAsync(ValidateMarketStopSafePriceFromLastPricePercentAsync)
            .When(x => x.EnableTrailingStops && x.MarketStopSafePriceFromLastPricePercent.HasValue);

        RuleFor(x => x.MarketStopExitRoeActivation)
            .CustomAsync(ValidateMarketStopExitRoeActivationAsync)
            .When(x => x.EnableMarketStopToExit);
        
        RuleFor(x => x.MarketStopExitPriceFromLastPricePercent)
            .CustomAsync(ValidateMarketStopExitPriceFromLastPricePercentAsync)
            .When(x => x.EnableMarketStopToExit);
        
        RuleFor(x => x.MarketStopExitActivationFromAvailableBalancePercent)
            .CustomAsync(ValidateMarketStopExitActivationFromAvailableBalancePercentAsync)
            .When(x => x.EnableMarketStopToExit && x.MarketStopExitActivationFromAvailableBalancePercent.HasValue);
        
        RuleFor(x => x.MarketStopExitActivationAfterTime)
            .CustomAsync(ValidateMarketStopExitActivationAfterTimeAsync)
            .When(x => x.EnableMarketStopToExit && x.MarketStopExitActivationAfterTime.HasValue);
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
    
    private Task ValidateLeverageAsync(int leverage, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (leverage)
            {
                case < 1:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Leverage)], 
                        "Cannot be lower then 1."));
                    return Task.CompletedTask;
                case > 125:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Leverage)], 
                        "Cannot be higher then 125."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateLeverageAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.Leverage)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateMaximumPositionsAsync(int maximumPositions, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (maximumPositions)
            {
                case < 0:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositions)], 
                        "Cannot be lower then 0."));
                    return Task.CompletedTask;
                case > 1000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositions)], 
                        "Cannot be higher then 1000."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMaximumPositionsAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositions)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateMaximumPositionsPerIterationAsync(int maximumPositionsPerIteration, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (maximumPositionsPerIteration)
            {
                case < 0:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositionsPerIteration)], 
                        "Cannot be lower then 0."));
                    return Task.CompletedTask;
                case > 1000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositionsPerIteration)], 
                        "Cannot be higher then 1000."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMaximumPositionsPerIterationAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositionsPerIteration)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateAvailableDepositPercentForTradingAsync(decimal availableDepositPercentForTrading, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (availableDepositPercentForTrading)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AvailableDepositPercentForTrading)], 
                        "Cannot be lower then 0.01."));
                    return Task.CompletedTask;
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AvailableDepositPercentForTrading)], 
                        "Cannot be higher then 100.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateAvailableDepositPercentForTradingAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.AvailableDepositPercentForTrading)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidatePercentFromDepositForOpenAsync(decimal percentFromDepositForOpen, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (percentFromDepositForOpen)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.PercentFromDepositForOpen)], 
                        "Cannot be lower then 0.01."));
                    return Task.CompletedTask;
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.PercentFromDepositForOpen)], 
                        "Cannot be higher then 100.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidatePercentFromDepositForOpenAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.PercentFromDepositForOpen)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }

    private Task ValidateMinQuoteVolumeForOpenAsync(decimal minQuoteVolumeForOpen, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (minQuoteVolumeForOpen)
            {
                case < 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForOpen)], 
                        "Cannot be lower then 0.00."));
                    return Task.CompletedTask;
                case > 100000000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForOpen)], 
                        "Cannot be higher then 100000000.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMinQuoteVolumeForOpenAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForOpen)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }

    private Task ValidateAverageToRoeAsync(decimal averageToRoe, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (averageToRoe)
            {
                case < -10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageToRoe)], 
                        "Cannot be lower then -10000.00."));
                    return Task.CompletedTask;
                case > 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageToRoe)], 
                        "Cannot be higher then 0.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateAverageToRoeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.AverageToRoe)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateAverageFromRoeAsync(decimal averageFromRoe, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (averageFromRoe)
            {
                case < -10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageFromRoe)], 
                        "Cannot be lower then -10000.00."));
                    return Task.CompletedTask;
                case > 10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageFromRoe)], 
                        "Cannot be higher then 10000.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateAverageFromRoeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.AverageFromRoe)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }

    private Task ValidateMinQuoteVolumeForAverageAsync(decimal minQuoteVolumeForAverage, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (minQuoteVolumeForAverage)
            {
                case < 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForAverage)], 
                        "Cannot be lower then 0.00."));
                    return Task.CompletedTask;
                case > 100000000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForAverage)], 
                        "Cannot be higher then 100000000.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMinQuoteVolumeForAverageAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForAverage)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateTrailingStopRoeAsync(decimal trailingStopRoe, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (trailingStopRoe)
            {
                case < -10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.TrailingStopRoe)], 
                        "Cannot be lower then -10000.00."));
                    return Task.CompletedTask;
                case > 10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.TrailingStopRoe)], 
                        "Cannot be higher then 10000.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTrailingStopRoeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.TrailingStopRoe)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateCallbackRateAsync(decimal callbackRate, ValidationContext<PercentLimitTradeLogicDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (callbackRate)
            {
                case < 0.1m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.CallbackRate)], 
                        "Cannot be lower then 0.1."));
                    return Task.CompletedTask;
                case > 5.0m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.CallbackRate)], 
                        "Cannot be higher then 5.0."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateCallbackRateAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.CallbackRate)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }

    private Task ValidateMarketStopSafePriceFromLastPricePercentAsync(decimal? marketStopSafePriceFromLastPricePercent, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopSafePriceFromLastPricePercent)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopSafePriceFromLastPricePercent)], 
                        "Cannot be lower then 0.01."));
                    return Task.CompletedTask;
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopSafePriceFromLastPricePercent)], 
                        "Cannot be higher then 100.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopSafePriceFromLastPricePercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopSafePriceFromLastPricePercent)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateMarketStopExitRoeActivationAsync(decimal marketStopExitRoeActivation, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopExitRoeActivation)
            {
                case < -10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitRoeActivation)], 
                        "Cannot be lower then -10000.00."));
                    return Task.CompletedTask;
                case > 10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitRoeActivation)], 
                        "Cannot be higher then 10000.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitRoeActivationAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitRoeActivation)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateMarketStopExitPriceFromLastPricePercentAsync(decimal marketStopExitRoeActivation, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopExitRoeActivation)
            {
                case < 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitPriceFromLastPricePercent)], 
                        "Cannot be lower then 0.00."));
                    return Task.CompletedTask;
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitPriceFromLastPricePercent)], 
                        "Cannot be higher then 100.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitPriceFromLastPricePercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitPriceFromLastPricePercent)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateMarketStopExitActivationFromAvailableBalancePercentAsync(decimal? marketStopExitActivationFromAvailableBalancePercent, 
        ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopExitActivationFromAvailableBalancePercent)
            {
                case < 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationFromAvailableBalancePercent)], 
                        "Cannot be lower then 0.00."));
                    return Task.CompletedTask;
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationFromAvailableBalancePercent)], 
                        "Cannot be higher then 100.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitActivationFromAvailableBalancePercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationFromAvailableBalancePercent)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
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
    
    #endregion
}