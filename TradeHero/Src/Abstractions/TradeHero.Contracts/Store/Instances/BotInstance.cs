using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Strategy;

namespace TradeHero.Contracts.Store.Instances;

public class BotInstance
{
    public StrategyStatus StrategyStatus { get; private set; } = StrategyStatus.Idle;
    public IStrategy? Strategy { get; private set; }

    public void SetStrategy(IStrategy? strategy, StrategyStatus strategyStatus)
    {
        Strategy = strategy;
        StrategyStatus = strategyStatus;
    }
}