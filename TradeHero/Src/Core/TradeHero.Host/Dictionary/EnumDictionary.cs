using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Host.Dictionary;

internal class EnumDictionary
{
    public string GetTradeLogicTypeUserFriendlyName(TradeLogicType tradeLogicType)
    {
        switch (tradeLogicType)
        {
            case TradeLogicType.NoTradeLogic:
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
    
    public string GetStrategyObjectUserFriendlyName(StrategyObject strategyObject)
    {
        switch (strategyObject)
        {
            case StrategyObject.None:
                return "Do not add strategy";
            case StrategyObject.TradeLogic:
                return "Trade logic";
            case StrategyObject.Instance:
                return "Instance";
            default:
                throw new ArgumentOutOfRangeException(nameof(StrategyObject), strategyObject, null);
        }
    }
}