using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Store;

namespace TradeHero.Services.Services;

internal class StoreService : IStoreService
{
    public ApplicationInfo Application { get; } = new();
    public BotInfo Bot { get; } = new();
}