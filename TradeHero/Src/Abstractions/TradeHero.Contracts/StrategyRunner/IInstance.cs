using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.StrategyRunner.Models.Instance;

namespace TradeHero.Contracts.StrategyRunner;

public interface IInstance
{
    Task<GenericBaseResult<InstanceResult>> GenerateInstanceResultAsync(ITradeLogicStore store, 
        BaseInstanceOptions instanceOptions, CancellationToken cancellationToken);
}