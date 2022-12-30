using FluentValidation;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Repositories;
using TradeHero.EntryPoint.Data.Dtos.Strategy;

namespace TradeHero.EntryPoint.Data.Validations;

internal class PercentMoveStrategyDtoValidation : AbstractValidator<PercentMoveStrategyDto>
{
    private readonly IStrategyRepository _strategyRepository;
    
    public PercentMoveStrategyDtoValidation(
        IStrategyRepository strategyRepository
        )
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

    private async Task<bool> CheckIsNameDoesNotExistInDatabaseForCreate(PercentMoveStrategyDto percentMoveStrategyDto, 
        string name, CancellationToken cancellationToken)
    {
        return await _strategyRepository.IsNameExistInDatabaseForCreate(name);
    }
    
    private async Task<bool> CheckIsNameDoesNotExistInDatabaseForUpdate(PercentMoveStrategyDto percentMoveStrategyDto, 
        string name, CancellationToken cancellationToken)
    {
        return await _strategyRepository.IsNameExistInDatabaseForUpdate(percentMoveStrategyDto.Id, name);
    }

    private void GeneralRules()
    {
        RuleFor(x => x.Name)
            .MinimumLength(3)
            .MaximumLength(40)
            .NotEmpty();
        
        RuleFor(x => x.PricePercentMove)
            .GreaterThanOrEqualTo(0.00m)
            .LessThanOrEqualTo(1000.00m);
    }
    
    #endregion
}