using TradeHero.Contracts.StrategyRunner.Models.Instance;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.StrategyRunner;

public interface IInstanceFactory
{
    InstanceFactoryResponse? GetInstance(InstanceType instanceType);
}