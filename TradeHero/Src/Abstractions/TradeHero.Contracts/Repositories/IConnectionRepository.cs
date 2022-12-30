﻿using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.Contracts.Repositories;

public interface IConnectionRepository
{
    ConnectionDto GetActiveConnection();
    Task<ConnectionDto> GetActiveConnectionAsync();
    Task AddConnectionAsync(ConnectionDto connectionDto);
    Task UpdateConnectionAsync(ConnectionDto connectionDto);
    Task DeleteConnectionAsync(ConnectionDto connectionDto);
}