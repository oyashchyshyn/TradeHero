using TradeHero.Contracts.Services.Models.Store;

namespace TradeHero.Contracts.Services;

public interface IStore
{
    BotInstance Bot { get; }
    InformationInstance Information { get; }
}