using TradeHero.Core.Types.Services;
using TradeHero.Core.Types.Services.Models.Store;

namespace TradeHero.Services.Services;

internal class StoreService : IStoreService
{
    public ApplicationInfo Application { get; } = new();
    public BotInfo Bot { get; } = new();
}