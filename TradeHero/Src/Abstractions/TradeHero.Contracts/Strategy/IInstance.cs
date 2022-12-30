using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Strategy.Models.Instance;

namespace TradeHero.Contracts.Strategy;

public interface IInstance
{
    Task<GenericBaseResult<InstanceResult>> GenerateInstanceResultAsync(IStrategyStore store, 
        BaseInstanceOptions instanceOptions, CancellationToken cancellationToken);
}