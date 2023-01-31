using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Models.Store;

namespace TradeHero.Services.Services;

internal class StoreService : IStoreService
{
    public ApplicationInfo Application { get; } = new();
    public BotInfo Bot { get; } = new();
}