using TradeHero.Core.Types.Services.Models.Store;

namespace TradeHero.Core.Types.Services;

public interface IStoreService
{
    ApplicationInfo Application { get; }
    BotInfo Bot { get; }
}