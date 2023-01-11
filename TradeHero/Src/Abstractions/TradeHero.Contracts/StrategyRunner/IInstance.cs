using TradeHero.Contracts.StrategyRunner.Models.Instance;
using TradeHero.Core.Models;

namespace TradeHero.Contracts.StrategyRunner;

public interface IInstance
{
    Task<GenericBaseResult<InstanceResult>> GenerateInstanceResultAsync(ITradeLogicStore store, 
        BaseInstanceOptions instanceOptions, CancellationToken cancellationToken);
}