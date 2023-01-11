using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Store;

namespace TradeHero.Services.Services;

internal class StoreService : IStore
{
    public BotInstance Bot { get; } = new();
    public InformationInstance Information { get; } = new();
}