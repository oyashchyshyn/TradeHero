using TradeHero.Core.Models;
using TradeHero.Core.Models.Trading;

namespace TradeHero.Core.Contracts.Trading;

public interface IInstance
{
    Task<GenericBaseResult<InstanceResult>> GenerateInstanceResultAsync(ITradeLogicStore store, 
        BaseInstanceOptions instanceOptions, CancellationToken cancellationToken);
}