using TradeHero.Contracts.Store.Instances;

namespace TradeHero.Contracts.Store;

public interface IStore
{
    BotInstance Bot { get; }
    InformationInstance Information { get; }
}