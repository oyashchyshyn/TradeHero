using TradeHero.Contracts.Trading.Models.Instance;
using TradeHero.Core.Models;

namespace TradeHero.Contracts.Trading;

public interface IInstance
{
    Task<GenericBaseResult<InstanceResult>> GenerateInstanceResultAsync(ITradeLogicStore store, 
        BaseInstanceOptions instanceOptions, CancellationToken cancellationToken);
}