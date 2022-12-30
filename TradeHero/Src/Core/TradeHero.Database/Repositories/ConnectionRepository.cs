using Microsoft.EntityFrameworkCore;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Database.Context;
using TradeHero.Database.Entities;

namespace TradeHero.Database.Repositories;

internal class ConnectionRepository : IConnectionRepository
{
    private readonly ThDatabaseContext _database;

    public ConnectionRepository(ThDatabaseContext database)
    {
        _database = database;
    }
    
    public ConnectionDto GetActiveConnection()
    {
        var activeConnection = _database.Connections.AsNoTracking().Single(x => x.IsActive);

        return GenerateConnectionDto(activeConnection);
    }
    
    public async Task<ConnectionDto> GetActiveConnectionAsync()
    {
        var activeConnection = await _database.Connections.AsNoTracking().SingleAsync(x => x.IsActive);

        return GenerateConnectionDto(activeConnection);
    }

    public Task AddConnectionAsync(ConnectionDto connectionDto)
    {
        throw new NotImplementedException();
    }

    public Task UpdateConnectionAsync(ConnectionDto connectionDto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteConnectionAsync(ConnectionDto connectionDto)
    {
        throw new NotImplementedException();
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
            IsActive = connection.IsActive
        };
    }

    #endregion
}