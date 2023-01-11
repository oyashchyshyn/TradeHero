using TradeHero.Contracts.Services.Models.Store;

namespace TradeHero.Contracts.Services;

public interface IStoreService
{
    BotInstance Bot { get; }
    InformationInstance Information { get; }
}