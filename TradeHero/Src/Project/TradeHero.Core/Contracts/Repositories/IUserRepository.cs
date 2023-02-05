using TradeHero.Core.Models.Repositories;

namespace TradeHero.Core.Contracts.Repositories;

public interface IUserRepository
{
    Task<UserDto?> GetActiveUserAsync();
    UserDto? GetActiveUser();
    Task<UserDto?> AddUserAsync(UserDto userDto);
    Task<bool> SetUserActiveAsync(Guid userId);
}