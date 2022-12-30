using FluentValidation;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Repositories;
using TradeHero.EntryPoint.Data.Dtos.Strategy;

namespace TradeHero.EntryPoint.Data.Validations;

internal class PercentLimitStrategyDtoValidation : AbstractValidator<PercentLimitStrategyDto>
{
    private readonly IStrategyRepository _strategyRepository;
    
    public PercentLimitStrategyDtoValidation(IStrategyRepository strategyRepository) 
    {
        _strategyRepository = strategyRepository;
        
        RuleSet(ValidationRuleSet.Create.ToString(), () =>
        {
            RuleFor(x => x.Name)
                .MustAsync(CheckIsNameDoesNotExistInDatabaseForCreate)
                .WithMessage(x => $"Strategy with name '{x.Name}' already exist.");
            
            GeneralRules();
        });
        
        RuleSet(ValidationRuleSet.Update.ToString(), () =>
        {
            RuleFor(x => x.Name)
                .MustAsync(CheckIsNameDoesNotExistInDatabaseForUpdate)
                .WithMessage(x => $"Strategy with name '{x.Name}' already exist.");

            GeneralRules();
        });
    }

    #region Private methods

    private async Task<bool> CheckIsNameDoesNotExistInDatabaseForCreate(PercentLimitStrategyDto percentLimitStrategyDto, 
        string name, CancellationToken cancellationToken)
    {
        return !await _strategyRepository.IsNameExistInDatabaseForCreate(name);
    }
    
    private async Task<bool> CheckIsNameDoesNotExistInDatabaseForUpdate(PercentLimitStrategyDto percentLimitStrategyDto, 
        string name, CancellationToken cancellationToken)
    {
        return !await _strategyRepository.IsNameExistInDatabaseForUpdate(percentLimitStrategyDto.Id, name);
    }

    private void GeneralRules()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(40);
        
        RuleFor(x => x.Leverage)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(125);
        
        RuleFor(x => x.MaximumPositions)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1000);
        
        RuleFor(x => x.MaximumPositionsPerIteration)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(1000);
        
        RuleFor(x => x.AvailableDepositPercentForTrading)
            .GreaterThanOrEqualTo(0.01m)
            .LessThanOrEqualTo(100.00m);
        
        RuleFor(x => x.PercentFromDepositForOpen)
            .GreaterThanOrEqualTo(0.01m)
            .LessThanOrEqualTo(100.00m);

        RuleFor(x => x.MinTradesForOpen)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1000000);
        
        RuleFor(x => x.MinQuoteVolumeForOpen)
            .GreaterThanOrEqualTo(0.00m)
            .LessThanOrEqualTo(100000000.00m);

        RuleFor(x => x.AverageToRoe)
            .GreaterThanOrEqualTo(-10000.00m)
            .LessThanOrEqualTo(0.00m);
        
        RuleFor(x => x.AverageFromRoe)
            .GreaterThanOrEqualTo(-10000.00m)
            .LessThanOrEqualTo(-1.00m);
        
        RuleFor(x => x.MinTradesForAverage)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1000000);
        
        RuleFor(x => x.MinQuoteVolumeForAverage)
            .GreaterThanOrEqualTo(0.00m)
            .LessThanOrEqualTo(100000000.00m);
        
        RuleFor(x => x.TrailingStopRoe)
            .GreaterThanOrEqualTo(-10000.00m)
            .LessThanOrEqualTo(10000.00m);
        
        RuleFor(x => x.CallbackRate)
            .GreaterThanOrEqualTo(0.1m)
            .LessThanOrEqualTo(5.0m);
        
        RuleFor(x => x.TrailingStopRoe)
            .GreaterThanOrEqualTo(-10000.00m)
            .LessThanOrEqualTo(10000.00m);
        
        RuleFor(x => x.MarketStopSafePriceFromLastPricePercent)
            .GreaterThanOrEqualTo(0.1m)
            .LessThanOrEqualTo(5.0m);
        
        RuleFor(x => x.CallbackRate)
            .GreaterThanOrEqualTo(0.1m)
            .LessThanOrEqualTo(5.0m);

        RuleFor(x => x.MarketStopSafePriceFromLastPricePercent)
            .GreaterThanOrEqualTo(0.00m).When(x => x.MarketStopSafePriceFromLastPricePercent.HasValue)
            .LessThanOrEqualTo(100.00m).When(x => x.MarketStopSafePriceFromLastPricePercent.HasValue);
        
        RuleFor(x => x.MarketStopExitRoeActivation)
            .GreaterThanOrEqualTo(0.00m)
            .LessThanOrEqualTo(100.00m);
        
        RuleFor(x => x.MarketStopExitPriceFromLastPricePercent)
            .GreaterThanOrEqualTo(0.00m)
            .LessThanOrEqualTo(100.00m);
        
        RuleFor(x => x.MarketStopExitActivationFromAvailableBalancePercent)
            .GreaterThanOrEqualTo(0.00m).When(x => x.MarketStopExitActivationFromAvailableBalancePercent.HasValue)
            .LessThanOrEqualTo(100.00m).When(x => x.MarketStopExitActivationFromAvailableBalancePercent.HasValue);
        
        RuleFor(x => x.MarketStopExitActivationAfterTime)
            .GreaterThanOrEqualTo(TimeSpan.Parse("00:00:01")).When(x => x.MarketStopExitActivationAfterTime.HasValue)
            .LessThanOrEqualTo(TimeSpan.Parse("24:00:00")).When(x => x.MarketStopExitActivationAfterTime.HasValue);
    }

    #endregion
}