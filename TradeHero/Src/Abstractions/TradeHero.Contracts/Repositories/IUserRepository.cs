using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.Contracts.Repositories;

public interface IUserRepository
{
    UserDto GetUser();
    Task<UserDto> GetUserAsync();
}