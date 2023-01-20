using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Data.Dtos.TradeLogic;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Types.Repositories;

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
            .MustAsync(ValidateNameAsync);
        
        RuleFor(x => x.Leverage)
            .MustAsync(ValidateLeverageAsync);
        
        RuleFor(x => x.MaximumPositions)
            .MustAsync(ValidateMaximumPositionsAsync);
        
        RuleFor(x => x.MaximumPositionsPerIteration)
            .MustAsync(ValidateMaximumPositionsPerIterationAsync);
        
        RuleFor(x => x.AvailableDepositPercentForTrading)
            .MustAsync(ValidateAvailableDepositPercentForTradingAsync);
        
        RuleFor(x => x.PercentFromDepositForOpen)
            .MustAsync(ValidatePercentFromDepositForOpenAsync)
            .When(x => x.EnableOpenPositions);

        RuleFor(x => x.MinTradesForOpen)
            .MustAsync(ValidateMinTradesForOpenAsync)
            .When(x => x.EnableOpenPositions);
        
        RuleFor(x => x.MinQuoteVolumeForOpen)
            .MustAsync(ValidateMinQuoteVolumeForOpenAsync)
            .When(x => x.EnableOpenPositions);

        RuleFor(x => x.AverageToRoe)
            .MustAsync(ValidateAverageToRoeAsync)
            .When(x => x.EnableAveraging);
        
        RuleFor(x => x.AverageFromRoe)
            .MustAsync(ValidateAverageFromRoeAsync)
            .When(x => x.EnableAveraging);
        
        RuleFor(x => x.MinTradesForAverage)
            .MustAsync(ValidateMinTradesForAverageAsync)
            .When(x => x.EnableAveraging);
        
        RuleFor(x => x.MinQuoteVolumeForAverage)
            .MustAsync(ValidateMinQuoteVolumeForAverageAsync)
            .When(x => x.EnableAveraging);
        
        RuleFor(x => x.TrailingStopRoe)
            .MustAsync(ValidateTrailingStopRoeAsync)
            .When(x => x.EnableTrailingStops);
        
        RuleFor(x => x.CallbackRate)
            .MustAsync(ValidateCallbackRateAsync)
            .When(x => x.EnableTrailingStops);

        RuleFor(x => x.MarketStopSafePriceFromLastPricePercent)
            .MustAsync(ValidateMarketStopSafePriceFromLastPricePercentAsync)
            .When(x => x.EnableTrailingStops && x.MarketStopSafePriceFromLastPricePercent.HasValue);

        RuleFor(x => x.MarketStopExitRoeActivation)
            .MustAsync(ValidateMarketStopExitRoeActivationAsync)
            .When(x => x.EnableMarketStopToExit);
        
        RuleFor(x => x.MarketStopExitPriceFromLastPricePercent)
            .MustAsync(ValidateMarketStopExitPriceFromLastPricePercentAsync)
            .When(x => x.EnableMarketStopToExit);
        
        RuleFor(x => x.MarketStopExitActivationFromAvailableBalancePercent)
            .MustAsync(ValidateMarketStopExitActivationFromAvailableBalancePercentAsync)
            .When(x => x.EnableMarketStopToExit && x.MarketStopExitActivationFromAvailableBalancePercent.HasValue);
        
        RuleFor(x => x.MarketStopExitActivationAfterTime)
            .MustAsync(ValidateMarketStopExitActivationAfterTimeAsync)
            .When(x => x.EnableMarketStopToExit && x.MarketStopExitActivationAfterTime.HasValue);
    }

    private async Task<bool> ValidateNameAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        string name, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], "Cannot be empty."));
                
                return false;
            }

            switch (name.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], "Minimum length 3."));
                    return false;
                case > 40:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], "Maximum length 40."));
                    return false;
            }

            var databaseCheckResult = false;
            switch (_validationRuleSet)
            {
                case ValidationRuleSet.Create:
                    databaseCheckResult = await _strategyRepository.IsNameExistInDatabaseForCreate(name);
                    break;
                case ValidationRuleSet.Update:
                    databaseCheckResult = await _strategyRepository.IsNameExistInDatabaseForUpdate(percentLimitTradeLogicDto.Id, name);
                    break;
            }

            if (databaseCheckResult)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.Name)], $"Strategy with name '{name}' already exist."));

                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateNameAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.Name)]}", 
                "Validation failed."));
            
            return false;
        }
    }
    
    private Task<bool> ValidateLeverageAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        int leverage, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (leverage)
            {
                case < 1:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Leverage)], 
                        "Cannot be lower then 1."));
                    return Task.FromResult(false);
                case > 125:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.Leverage)], 
                        "Cannot be higher then 125."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateLeverageAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.Leverage)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMaximumPositionsAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        int maximumPositions, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (maximumPositions)
            {
                case < 0:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositions)], 
                        "Cannot be lower then 0."));
                    return Task.FromResult(false);
                case > 1000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositions)], 
                        "Cannot be higher then 1000."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMaximumPositionsAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositions)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMaximumPositionsPerIterationAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        int maximumPositionsPerIteration, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (maximumPositionsPerIteration)
            {
                case < 1:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositionsPerIteration)], 
                        "Cannot be lower then 1."));
                    return Task.FromResult(false);
                case > 1000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositionsPerIteration)], 
                        "Cannot be higher then 1000."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMaximumPositionsPerIterationAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MaximumPositionsPerIteration)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateAvailableDepositPercentForTradingAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal availableDepositPercentForTrading, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (availableDepositPercentForTrading)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AvailableDepositPercentForTrading)], 
                        "Cannot be lower then 0.01."));
                    return Task.FromResult(false);
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AvailableDepositPercentForTrading)], 
                        "Cannot be higher then 100.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateAvailableDepositPercentForTradingAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.AvailableDepositPercentForTrading)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidatePercentFromDepositForOpenAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal percentFromDepositForOpen, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (percentFromDepositForOpen)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.PercentFromDepositForOpen)], 
                        "Cannot be lower then 0.01."));
                    return Task.FromResult(false);
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.PercentFromDepositForOpen)], 
                        "Cannot be higher then 100.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidatePercentFromDepositForOpenAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.PercentFromDepositForOpen)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMinTradesForOpenAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        int minTradesForOpen, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (minTradesForOpen)
            {
                case < 0:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinTradesForOpen)], 
                        "Cannot be lower then 0."));
                    return Task.FromResult(false);
                case > 1000000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinTradesForOpen)], 
                        "Cannot be higher then 1000000."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMinTradesForOpenAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MinTradesForOpen)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMinQuoteVolumeForOpenAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal minQuoteVolumeForOpen, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (minQuoteVolumeForOpen)
            {
                case < 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForOpen)], 
                        "Cannot be lower then 0.00."));
                    return Task.FromResult(false);
                case > 100000000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForOpen)], 
                        "Cannot be higher then 100000000.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMinQuoteVolumeForOpenAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForOpen)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }

    private Task<bool> ValidateAverageToRoeAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal averageToRoe, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (averageToRoe)
            {
                case < -10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageToRoe)], 
                        "Cannot be lower then -10000.00."));
                    return Task.FromResult(false);
                case > 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageToRoe)], 
                        "Cannot be higher then 0.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateAverageToRoeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.AverageToRoe)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateAverageFromRoeAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal averageFromRoe, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (averageFromRoe)
            {
                case < -10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageFromRoe)], 
                        "Cannot be lower then -10000.00."));
                    return Task.FromResult(false);
                case > 10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.AverageFromRoe)], 
                        "Cannot be higher then 10000.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateAverageFromRoeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.AverageFromRoe)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMinTradesForAverageAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        int minTradesForOpen, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (minTradesForOpen)
            {
                case < 0:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinTradesForAverage)], 
                        "Cannot be lower then 0."));
                    return Task.FromResult(false);
                case > 1000000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinTradesForAverage)], 
                        "Cannot be higher then 1000000."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMinTradesForAverageAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MinTradesForAverage)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMinQuoteVolumeForAverageAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal minQuoteVolumeForAverage, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (minQuoteVolumeForAverage)
            {
                case < 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForAverage)], 
                        "Cannot be lower then 0.00."));
                    return Task.FromResult(false);
                case > 100000000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForAverage)], 
                        "Cannot be higher then 100000000.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMinQuoteVolumeForAverageAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MinQuoteVolumeForAverage)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateTrailingStopRoeAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal trailingStopRoe, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (trailingStopRoe)
            {
                case < -10000.0m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.TrailingStopRoe)], 
                        "Cannot be lower then -10000.0."));
                    return Task.FromResult(false);
                case > 10000.0m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.TrailingStopRoe)], 
                        "Cannot be higher then 10000.0."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTrailingStopRoeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.TrailingStopRoe)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateCallbackRateAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal callbackRate, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (callbackRate)
            {
                case < 0.1m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.CallbackRate)], 
                        "Cannot be lower then 0.1."));
                    return Task.FromResult(false);
                case > 5.0m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.CallbackRate)], 
                        "Cannot be higher then 5.0."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateCallbackRateAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.CallbackRate)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }

    private Task<bool> ValidateMarketStopSafePriceFromLastPricePercentAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal? marketStopSafePriceFromLastPricePercent, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopSafePriceFromLastPricePercent)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopSafePriceFromLastPricePercent)], 
                        "Cannot be lower then 0.01."));
                    return Task.FromResult(false);
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopSafePriceFromLastPricePercent)], 
                        "Cannot be higher then 100.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopSafePriceFromLastPricePercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopSafePriceFromLastPricePercent)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMarketStopExitRoeActivationAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal marketStopExitRoeActivation, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopExitRoeActivation)
            {
                case < -10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitRoeActivation)], 
                        "Cannot be lower then -10000.00."));
                    return Task.FromResult(false);
                case > 10000.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitRoeActivation)], 
                        "Cannot be higher then 10000.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitRoeActivationAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitRoeActivation)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMarketStopExitPriceFromLastPricePercentAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal marketStopExitRoeActivation, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopExitRoeActivation)
            {
                case < 0.0m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitPriceFromLastPricePercent)], 
                        "Cannot be lower then 0.0."));
                    return Task.FromResult(false);
                case > 100.0m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitPriceFromLastPricePercent)], 
                        "Cannot be higher then 100.0."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitPriceFromLastPricePercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitPriceFromLastPricePercent)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMarketStopExitActivationFromAvailableBalancePercentAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        decimal? marketStopExitActivationFromAvailableBalancePercent, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (marketStopExitActivationFromAvailableBalancePercent)
            {
                case < 0.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationFromAvailableBalancePercent)], 
                        "Cannot be lower then 0.00."));
                    return Task.FromResult(false);
                case > 100.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationFromAvailableBalancePercent)], 
                        "Cannot be higher then 100.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitActivationFromAvailableBalancePercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationFromAvailableBalancePercent)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateMarketStopExitActivationAfterTimeAsync(PercentLimitTradeLogicDto percentLimitTradeLogicDto, 
        TimeSpan? marketStopExitActivationAfterTime, ValidationContext<PercentLimitTradeLogicDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (!marketStopExitActivationAfterTime.HasValue)
            {
                return Task.FromResult(true);
            }

            if (marketStopExitActivationAfterTime.Value < TimeSpan.Parse("00:00:01"))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationAfterTime)], 
                    "Cannot be lower then 00:00:01."));
                
                return Task.FromResult(false);
            }
            
            if (marketStopExitActivationAfterTime.Value > TimeSpan.Parse("24:00:00"))
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationAfterTime)], 
                    "Cannot be higher then 24:00:00."));
                
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateMarketStopExitActivationAfterTimeAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(PercentLimitTradeLogicDto.MarketStopExitActivationAfterTime)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    #endregion
}