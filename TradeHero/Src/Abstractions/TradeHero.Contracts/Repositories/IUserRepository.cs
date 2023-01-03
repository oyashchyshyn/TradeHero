using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.Contracts.Repositories;

public interface IUserRepository
{
    Task<UserDto?> GetActiveUserAsync();
    UserDto? GetActiveUser();
    Task<bool> AddUserAsync(UserDto userDto);
}