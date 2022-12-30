using TradeHero.Contracts.Store;
using TradeHero.Contracts.Store.Instances;

namespace TradeHero.Core.Store;

internal class ApplicationStore : IStore
{
    public BotInstance Bot { get; } = new();
    public InformationInstance Information { get; } = new();
}