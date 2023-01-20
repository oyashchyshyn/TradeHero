using TradeHero.Core.Models;
using TradeHero.Core.Types.Trading.Models.Instance;

namespace TradeHero.Core.Types.Trading;

public interface IInstance
{
    Task<GenericBaseResult<InstanceResult>> GenerateInstanceResultAsync(ITradeLogicStore store, 
        BaseInstanceOptions instanceOptions, CancellationToken cancellationToken);
}