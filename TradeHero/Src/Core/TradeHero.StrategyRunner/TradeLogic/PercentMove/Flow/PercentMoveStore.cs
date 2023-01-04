using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.StrategyRunner.Base;
using TradeHero.StrategyRunner.TradeLogic.PercentMove.Options;

namespace TradeHero.StrategyRunner.TradeLogic.PercentMove.Flow;

internal class PercentMoveStore : BaseTradeLogicStore
{
    public PercentMoveTradeLogicOptions TradeLogicOptions { get; private set; } = null!;

    public PercentMoveStore(
        ILogger<PercentMoveStore> logger, 
        IJsonService jsonService
        ) 
        : base(logger, jsonService)
    { }
    
    public Dictionary<string, bool> SymbolStatus { get; } = new();
    public Dictionary<string, decimal> SymbolLastOrderPrice { get; } = new();

    public override ActionResult AddTradeLogicOptions(StrategyDto strategyDto)
    {
        try
        {
            Logger.LogInformation("Trade logic options from database: {Data}. In {Method}", 
                strategyDto.TradeLogicJson, nameof(AddTradeLogicOptions));
            
            var tradeOptionsConvertedData = JsonService.Deserialize<PercentMoveTradeLogicOptions>(strategyDto.TradeLogicJson);
            if (tradeOptionsConvertedData.ActionResult != ActionResult.Success)
            {
                Logger.LogError("Cannot deserialize Trade logic options. In {Method}", nameof(AddTradeLogicOptions));
                
                return ActionResult.Error;
            }
        
            TradeLogicOptions = tradeOptionsConvertedData.Data;

            Logger.LogInformation("Trade logic options after converting: {Data}. In {Method}", 
                JsonService.SerializeObject(TradeLogicOptions).Data, nameof(AddTradeLogicOptions));
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(AddTradeLogicOptions));
                
            return ActionResult.Error;
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
            
            SymbolStatus.Clear();
            SymbolLastOrderPrice.Clear();
            TradeLogicOptions = new PercentMoveTradeLogicOptions();

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(ClearDataAsync));
            
            return ActionResult.SystemError;
        }
    }
}