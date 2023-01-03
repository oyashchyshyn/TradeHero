using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.Contracts.Repositories;

public interface IUserRepository
{
    Task<UserDto?> GetActiveUserAsync();
    UserDto? GetActiveUser();
    Task<UserDto?> AddUserAsync(UserDto userDto);
    Task<bool> SetUserActiveAsync(Guid userId);
}