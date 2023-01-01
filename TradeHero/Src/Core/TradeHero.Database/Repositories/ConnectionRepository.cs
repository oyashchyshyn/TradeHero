using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.Database.Context;
using TradeHero.Database.Entities;

namespace TradeHero.Database.Repositories;

internal class ConnectionRepository : IConnectionRepository
{
    private readonly ILogger<ConnectionRepository> _logger;
    private readonly ThDatabaseContext _database;
    private readonly IDateTimeService _dateTimeService;

    public ConnectionRepository(
        ILogger<ConnectionRepository> logger, 
        ThDatabaseContext database, 
        IDateTimeService dateTimeService
        )
    {
        _logger = logger;
        _database = database;
        _dateTimeService = dateTimeService;
    }
    
    public async Task<List<ConnectionDto>> GetConnectionsAsync()
    {
        try
        {
            return await _database.Connections.AsNoTracking()
                .Select(x => GenerateConnectionDto(x))
                .ToListAsync();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetConnectionsAsync));
            
            return new List<ConnectionDto>();
        }
    }
    
    public async Task<ConnectionDto?> GetConnectionByIdAsync(Guid connectionId)
    {
        try
        {
            var connection = await _database.Connections.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == connectionId);
        
            return connection != null ? GenerateConnectionDto(connection) : null;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetConnectionByIdAsync));

            return null;
        }
    }
    
    public ConnectionDto? GetActiveConnection()
    {
        try
        {
            var activeConnection = _database.Connections.AsNoTracking().SingleOrDefault(x => x.IsActive);

            return activeConnection == null ? null : GenerateConnectionDto(activeConnection);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetActiveConnection));

            return null;
        }
    }

    public async Task<ConnectionDto?> GetActiveConnectionAsync()
    {
        try
        {
            var activeConnection = await _database.Connections.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive);

            return activeConnection == null ? null : GenerateConnectionDto(activeConnection);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetActiveConnectionAsync));

            return null;
        }
    }

    public async Task<bool> SetActiveConnectionAsync(Guid connectionId)
    {
        try
        {
            var connections = await _database.Connections.ToListAsync();

            foreach (var connection in connections)
            {
                connection.IsActive = connection.Id == connectionId;
            }

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetActiveConnectionAsync));

            return false;
        }
    }
    
    public async Task<bool> AddConnectionAsync(ConnectionDto connectionDto)
    {
        try
        {
            var newConnection = new Connection
            {
                Name = connectionDto.Name,
                ApiKey = connectionDto.ApiKey,
                SecretKey = connectionDto.SecretKey,
                IsActive = connectionDto.IsActive,
                CreationDateTime = _dateTimeService.GetUtcDateTime()
            };
        
            await _database.Connections.AddAsync(newConnection);

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(DeleteConnectionAsync));

            return false;
        }
    }

    public async Task<bool> DeleteConnectionAsync(Guid connectionId)
    {
        try
        {
            var connection = await _database.Connections.SingleAsync(x => x.Id == connectionId);

            _database.Connections.Remove(connection);

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(DeleteConnectionAsync));

            return false;
        }
    }

    public async Task<bool> IsNameExistInDatabaseForCreate(string name)
    {
        return await _database.Connections.AnyAsync(x => x.Name == name);
    }
    
    public async Task<bool> IsNameExistInDatabaseForUpdate(Guid id, string name)
    {
        return await _database.Connections.AnyAsync(x => x.Id != id && x.Name == name);
    }

    #region private methods

    private static ConnectionDto GenerateConnectionDto(Connection connection)
    {
        return new ConnectionDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ApiKey = connection.ApiKey,
            SecretKey = connection.SecretKey,
            CreationDateTime = connection.CreationDateTime,
            IsActive = connection.IsActive
        };
    }

    #endregion
}