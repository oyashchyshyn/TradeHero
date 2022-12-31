using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Database.Context;
using TradeHero.Database.Entities;

namespace TradeHero.Database.Repositories;

internal class StrategyRepository : IStrategyRepository
{
    private readonly ILogger<StrategyRepository> _logger;
    private readonly ThDatabaseContext _database;

    public StrategyRepository(
        ILogger<StrategyRepository> logger, 
        ThDatabaseContext database)
    {
        _logger = logger;
        _database = database;
    }

    public async Task<List<StrategyDto>> GetStrategiesAsync()
    {
        try
        {
            return await _database.Strategies.AsNoTracking()
                .Select(x => GenerateStrategyDto(x))
                .ToListAsync();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetStrategiesAsync));
            
            return new List<StrategyDto>();
        }
    }

    public async Task<StrategyDto?> GetStrategyByIdAsync(Guid strategyId)
    {
        try
        {
            var strategy = await _database.Strategies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == strategyId);
        
            return strategy != null ? GenerateStrategyDto(strategy) : null;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetStrategyByIdAsync));

            return null;
        }
    }

    public async Task<StrategyDto?> GetActiveStrategyAsync()
    {
        try
        {
            var activeStrategy = await _database.Strategies.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IsActive);

            return activeStrategy != null ? GenerateStrategyDto(activeStrategy) : null;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetActiveStrategyAsync));

            return null;
        }
    }

    public async Task<bool> SetActiveStrategyAsync(Guid strategyId)
    {
        try
        {
            var strategies = await _database.Strategies.ToListAsync();

            foreach (var strategy in strategies)
            {
                strategy.IsActive = strategy.Id == strategyId;
            }

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetActiveStrategyAsync));

            return false;
        }
    }

    public async Task<bool> AddStrategyAsync(StrategyDto strategyDto)
    {
        try
        {
            var newStrategy = new Strategy
            {
                Name = strategyDto.Name,
                TradeLogicType = strategyDto.TradeLogicType,
                InstanceType = strategyDto.InstanceType,
                TradeLogicJson = strategyDto.TradeLogicJson,
                InstanceJson = strategyDto.InstanceJson,
                IsActive = strategyDto.IsActive
            };
        
            await _database.Strategies.AddAsync(newStrategy);

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(AddStrategyAsync));

            return false;
        }
    }
    
    public async Task<bool> UpdateStrategyAsync(StrategyDto strategyDto)
    {
        try
        {
            var strategy = await _database.Strategies.SingleAsync(x => x.Id == strategyDto.Id);
        
            strategy.Name = strategyDto.Name;
            strategy.TradeLogicType = strategyDto.TradeLogicType;
            strategy.InstanceType = strategyDto.InstanceType;
            strategy.InstanceJson = strategyDto.InstanceJson;
            strategy.TradeLogicJson = strategyDto.TradeLogicJson;
            strategy.IsActive = strategyDto.IsActive;

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(UpdateStrategyAsync));

            return false;
        }
    }

    public async Task<bool> DeleteStrategyAsync(Guid strategyId)
    {
        try
        {
            var strategy = await _database.Strategies.SingleAsync(x => x.Id == strategyId);

            _database.Strategies.Remove(strategy);

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(DeleteStrategyAsync));

            return false;
        }
    }

    public async Task<bool> IsNameExistInDatabaseForCreate(string name)
    {
        return await _database.Strategies.AnyAsync(x => x.Name == name);
    }
    
    public async Task<bool> IsNameExistInDatabaseForUpdate(Guid id, string name)
    {
        return await _database.Strategies.AnyAsync(x => x.Id != id && x.Name == name);
    }

    #region private methods

    private static StrategyDto GenerateStrategyDto(Strategy strategy)
    {
        return new StrategyDto
        {
            Id = strategy.Id,
            Name = strategy.Name,
            TradeLogicType = strategy.TradeLogicType,
            InstanceType = strategy.InstanceType,
            TradeLogicJson = strategy.TradeLogicJson,
            InstanceJson = strategy.InstanceJson,
            IsActive = strategy.IsActive
        };
    }

    #endregion
}