using TradeHero.Contracts.Base.Enums;

namespace TradeHero.EntryPoint.Dictionary;

internal class EnumDictionary
{
    public string GetStrategyTypeUserFriendlyName(StrategyType strategyType)
    {
        switch (strategyType)
        {
            case StrategyType.NoStrategy:
                return "Do not add strategy";
            case StrategyType.PercentLimit:
                return "Percent limit";
            case StrategyType.PercentMove:
                return "Percent move";
            default:
                throw new ArgumentOutOfRangeException(nameof(strategyType), strategyType, null);
        }
    }
    
    public string GetInstanceTypeUserFriendlyName(InstanceType instanceType)
    {
        switch (instanceType)
        {
            case InstanceType.NoInstance:
                return "Do not add instance";
            case InstanceType.SpotClusterVolume:
                return "Spot cluster volume";
            default:
                throw new ArgumentOutOfRangeException(nameof(instanceType), instanceType, null);
        }
    }
}