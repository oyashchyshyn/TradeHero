using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.StrategyRunner.Models.Instance;

namespace TradeHero.Contracts.StrategyRunner;

public interface IInstanceFactory
{
    InstanceFactoryResponse? GetInstance(InstanceType instanceType);
}