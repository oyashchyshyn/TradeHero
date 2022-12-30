using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.Contracts.Repositories;

public interface IStrategyRepository
{
    Task<List<StrategyDto>> GetStrategiesAsync();
    Task<StrategyDto?> GetStrategyByIdAsync(Guid strategyId);
    Task<StrategyDto?> GetActiveStrategyAsync();
    Task<bool> SetActiveStrategyAsync(Guid strategyId);
    Task<bool> AddStrategyAsync(StrategyDto strategyDto);
    Task<bool> UpdateStrategyAsync(StrategyDto strategyDto);
    Task<bool> DeleteStrategyAsync(Guid strategyId);
    Task<bool> IsNameExistInDatabaseForCreate(string name);
    Task<bool> IsNameExistInDatabaseForUpdate(Guid id, string name);
}