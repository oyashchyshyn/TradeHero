using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Trading.Base;
using TradeHero.Trading.TradeLogic.PercentLimit.Models;
using TradeHero.Trading.TradeLogic.PercentLimit.Options;

namespace TradeHero.Trading.TradeLogic.PercentLimit.Flow;

internal class PercentLimitStore : BaseTradeLogicStore
{
    public Dictionary<string, PercentLimitPositionInfo> PositionsInfo { get; } = new();
    public PercentLimitTradeLogicLogicOptions TradeLogicLogicOptions { get; private set; } = null!;

    public PercentLimitStore(
        ILogger<PercentLimitStore> logger, 
        IJsonService jsonService
        ) 
        : base(logger, jsonService)
    { }

    public override ActionResult AddTradeLogicOptions(StrategyDto strategyDto)
    {
        try
        {
            Logger.LogInformation("Trade logic options from database: {Data}. In {Method}", 
                strategyDto.TradeLogicJson, nameof(AddTradeLogicOptions));
            
            var tradeOptionsConvertedData = JsonService.Deserialize<PercentLimitTradeLogicLogicOptions>(strategyDto.TradeLogicJson);
            if (tradeOptionsConvertedData.ActionResult != ActionResult.Success)
            {
                Logger.LogError("Cannot deserialize Trade logic options. In {Method}", nameof(AddTradeLogicOptions));
                
                return ActionResult.Error;
            }
        
            TradeLogicLogicOptions = tradeOptionsConvertedData.Data;

            Logger.LogInformation("Trade logic options after converting: {Data}. In {Method}", 
                JsonService.SerializeObject(TradeLogicLogicOptions).Data, nameof(AddTradeLogicOptions));
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(AddTradeLogicOptions));
                
            return ActionResult.SystemError;
        }
    }

    public override async Task<ActionResult> ClearDataAsync()
    {
        try
        {
            var baseResult = await base.ClearDataAsync();
            if (baseResult != ActionResult.Success)
            {
                Logger.LogWarning("Error in base result. In {Method}", nameof(ClearDataAsync));
                
                return baseResult;
            }
            
            PositionsInfo.Clear();
            TradeLogicLogicOptions = new PercentLimitTradeLogicLogicOptions();
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(ClearDataAsync));
            
            return ActionResult.SystemError;
        }
    }
}