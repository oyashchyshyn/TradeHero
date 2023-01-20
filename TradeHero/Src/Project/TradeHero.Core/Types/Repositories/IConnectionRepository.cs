﻿using TradeHero.Core.Types.Repositories.Models;

namespace TradeHero.Core.Types.Repositories;

public interface IConnectionRepository
{
    Task<List<ConnectionDto>> GetConnectionsAsync();
    Task<ConnectionDto?> GetConnectionByIdAsync(Guid connectionId);
    ConnectionDto? GetActiveConnection();
    Task<ConnectionDto?> GetActiveConnectionAsync();
    Task<bool> SetActiveConnectionAsync(Guid connectionId);
    Task<bool> AddConnectionAsync(ConnectionDto connectionDto);
    Task<bool> DeleteConnectionAsync(Guid connectionId);
    Task<bool> IsNameExistInDatabaseForCreate(string name);
}