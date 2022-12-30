using Microsoft.EntityFrameworkCore;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Database.Context;
using TradeHero.Database.Entities;

namespace TradeHero.Database.Repositories;

internal class UserRepository : IUserRepository
{
    private readonly ThDatabaseContext _database;

    public UserRepository(ThDatabaseContext database)
    {
        _database = database;
    }
    
    public UserDto GetUser()
    {
        var user = _database.User.AsNoTracking().Single(x => x.IsActive);

        return GenerateUserDto(user);
    }
    
    public async Task<UserDto> GetUserAsync()
    {
        var user = await _database.User.AsNoTracking().SingleAsync(x => x.IsActive);

        return GenerateUserDto(user);
    }
    
    #region private methods

    private static UserDto GenerateUserDto(User user)
    {
        return new UserDto
        {
            TelegramUserId = user.TelegramUserId,
            TelegramBotToken = user.TelegramBotToken,
        };
    }

    #endregion
}