using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Repositories.Models;
using TradeHero.Database.Context;
using TradeHero.Database.Entities;

namespace TradeHero.Database.Repositories;

internal class UserRepository : IUserRepository
{
    private readonly ILogger<UserRepository> _logger;
    private readonly ThDatabaseContext _database;

    public UserRepository(
        ILogger<UserRepository> logger,
        ThDatabaseContext database
        )
    {
        _logger = logger;
        _database = database;
    }
    
    public UserDto? GetActiveUser()
    {
        try
        {
            var user = _database.Users.AsNoTracking().SingleOrDefault(x => x.IsActive);
            return user == null ? null : GenerateUserDto(user);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetActiveUser));

            return null;
        }
    }
    
    public async Task<UserDto?> GetActiveUserAsync()
    {
        try
        {
            var user = await _database.Users.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive);
            return user == null ? null : GenerateUserDto(user);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetActiveUserAsync));

            return null;
        }
    }
    
    public async Task<UserDto?> AddUserAsync(UserDto userDto)
    {
        try
        {
            var newUser = new User
            {
                Name = userDto.Name,
                TelegramUserId = userDto.TelegramUserId,
                TelegramBotToken = userDto.TelegramBotToken,
                IsActive = false
            };
        
            await _database.Users.AddAsync(newUser);

            return await _database.SaveChangesAsync() > 0 ? GenerateUserDto(newUser) : null;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(AddUserAsync));

            return null;
        }
    }
    
    public async Task<bool> SetUserActiveAsync(Guid userId)
    {
        try
        {
            var users = await _database.Users.ToListAsync();

            foreach (var user in users)
            {
                user.IsActive = user.Id == userId;
            }

            return await _database.SaveChangesAsync() >= 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SetUserActiveAsync));

            return false;
        }
    }

    #region private methods

    private static UserDto GenerateUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            TelegramUserId = user.TelegramUserId,
            TelegramBotToken = user.TelegramBotToken,
            IsActive = user.IsActive
        };
    }

    #endregion
}