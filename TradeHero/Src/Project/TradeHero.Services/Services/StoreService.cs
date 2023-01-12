using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Store;

namespace TradeHero.Services.Services;

internal class StoreServiceService : IStoreService
{
    public BotInstance Bot { get; } = new();
    public InformationInstance Information { get; } = new();
}