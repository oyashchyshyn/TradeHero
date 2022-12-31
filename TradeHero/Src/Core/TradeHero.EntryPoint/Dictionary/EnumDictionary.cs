using TradeHero.Contracts.Base.Enums;

namespace TradeHero.EntryPoint.Dictionary;

internal class EnumDictionary
{
    public string GetStrategyTypeUserFriendlyName(TradeLogicType tradeLogicType)
    {
        switch (tradeLogicType)
        {
            case TradeLogicType.NoStrategy:
                return "Do not add strategy";
            case TradeLogicType.PercentLimit:
                return "Percent limit";
            case TradeLogicType.PercentMove:
                return "Percent move";
            default:
                throw new ArgumentOutOfRangeException(nameof(tradeLogicType), tradeLogicType, null);
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