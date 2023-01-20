using TradeHero.Core.Enums;
using TradeHero.Core.Types.Trading.Models.Instance;

namespace TradeHero.Core.Types.Trading;

public interface IInstanceFactory
{
    InstanceFactoryResponse? GetInstance(InstanceType instanceType);
}