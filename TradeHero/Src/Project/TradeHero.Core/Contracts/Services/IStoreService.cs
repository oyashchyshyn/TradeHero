using TradeHero.Core.Models.Store;

namespace TradeHero.Core.Contracts.Services;

public interface IStoreService
{
    ApplicationInfo Application { get; }
    BotInfo Bot { get; }
}