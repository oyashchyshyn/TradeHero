using TradeHero.Contracts.Trading.Models.Instance;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Trading;

public interface IInstanceFactory
{
    InstanceFactoryResponse? GetInstance(InstanceType instanceType);
}