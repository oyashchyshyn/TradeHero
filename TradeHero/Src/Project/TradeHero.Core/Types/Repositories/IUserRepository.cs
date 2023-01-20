using TradeHero.Core.Types.Repositories.Models;

namespace TradeHero.Core.Types.Repositories;

public interface IUserRepository
{
    Task<UserDto?> GetActiveUserAsync();
    UserDto? GetActiveUser();
    Task<UserDto?> AddUserAsync(UserDto userDto);
    Task<bool> SetUserActiveAsync(Guid userId);
}