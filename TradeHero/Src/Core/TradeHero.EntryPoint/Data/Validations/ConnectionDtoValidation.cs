using FluentValidation;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.EntryPoint.Data.Validations;

internal class ConnectionDtoValidation : AbstractValidator<ConnectionDto>
{
    private readonly IConnectionRepository _strategyRepository;
    
    public ConnectionDtoValidation(IConnectionRepository strategyRepository) 
    {
        _strategyRepository = strategyRepository;
        
        RuleSet(ValidationRuleSet.Create.ToString(), () =>
        {
            RuleFor(x => x.Name)
                .MustAsync(CheckIsNameDoesNotExistInDatabaseForCreateAsync)
                .WithMessage(x => $"Strategy with name '{x.Name}' already exist.");
            
            GeneralRules();
        });
        
        RuleSet(ValidationRuleSet.Update.ToString(), () =>
        {
            RuleFor(x => x.Name)
                .MustAsync(CheckIsNameDoesNotExistInDatabaseForUpdateAsync)
                .WithMessage(x => $"Strategy with name '{x.Name}' already exist.");

            GeneralRules();
        });
    }
    
    #region Private methods

    private void GeneralRules()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(40);
        
        RuleFor(x => x.ApiKey)
            .NotEmpty();
        
        RuleFor(x => x.SecretKey)
            .NotEmpty();
        
        RuleFor(x => x)
            .MustAsync(CheckIsConnectionToExchangerValidAsync)
            .When(x => !string.IsNullOrWhiteSpace(x.ApiKey) && !string.IsNullOrWhiteSpace(x.SecretKey))
            .WithMessage("Cannot connect to exchanger.");
    }

    private async Task<bool> CheckIsNameDoesNotExistInDatabaseForCreateAsync(ConnectionDto percentLimitStrategyDto, 
        string name, CancellationToken cancellationToken)
    {
        return !await _strategyRepository.IsNameExistInDatabaseForCreate(name);
    }
    
    private async Task<bool> CheckIsNameDoesNotExistInDatabaseForUpdateAsync(ConnectionDto percentLimitStrategyDto, 
        string name, CancellationToken cancellationToken)
    {
        return !await _strategyRepository.IsNameExistInDatabaseForUpdate(percentLimitStrategyDto.Id, name);
    }
    
    private async Task<bool> CheckIsConnectionToExchangerValidAsync(ConnectionDto percentLimitStrategyDto, ConnectionDto percentLimitStrategyDtoT, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(percentLimitStrategyDto.ApiKey) || string.IsNullOrWhiteSpace(percentLimitStrategyDto.SecretKey))
        {
            return false;
        }
        
        return true;
    }
    
    #endregion
}