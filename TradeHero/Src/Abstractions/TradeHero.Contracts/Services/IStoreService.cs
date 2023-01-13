using TradeHero.Contracts.Services.Models.Store;

namespace TradeHero.Contracts.Services;

public interface IStoreService
{
    ApplicationInfo Application { get; }
    BotInfo Bot { get; }
}