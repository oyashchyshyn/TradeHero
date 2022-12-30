using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Strategy.Models.Instance;

namespace TradeHero.Contracts.Strategy;

public interface IInstanceFactory
{
    InstanceFactoryResponse? GetInstance(InstanceType instanceType);
}