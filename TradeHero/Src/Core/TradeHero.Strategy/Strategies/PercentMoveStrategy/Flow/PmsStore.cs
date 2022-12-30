using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.Strategies.Base;
using TradeHero.Strategies.Strategies.PercentMoveStrategy.Options;

namespace TradeHero.Strategies.Strategies.PercentMoveStrategy.Flow;

internal class PmsStore : BaseStrategyStore
{
    public PmsStrategyOptions StrategyOptions { get; private set; } = null!;

    public PmsStore(
        ILogger<PmsStore> logger, 
        IJsonService jsonService
        ) 
        : base(logger, jsonService)
    { }
    
    public Dictionary<string, bool> SymbolStatus { get; } = new();
    public Dictionary<string, decimal> SymbolLastOrderPrice { get; } = new();

    public override ActionResult AddStrategyOptions(StrategyDto strategyDto)
    {
        try
        {
            Logger.LogInformation("Trade options from database: {Data}. In {Method}", 
                strategyDto.StrategyJson, nameof(AddStrategyOptions));
            
            var tradeOptionsConvertedData = JsonService.Deserialize<PmsStrategyOptions>(strategyDto.StrategyJson);
            if (tradeOptionsConvertedData.ActionResult != ActionResult.Success)
            {
                Logger.LogError("Cannot deserialize TradeOptions. In {Method}", nameof(AddStrategyOptions));
                
                return ActionResult.Error;
            }
        
            StrategyOptions = tradeOptionsConvertedData.Data;

            Logger.LogInformation("Trade options after converting: {Data}. In {Method}", 
                JsonService.SerializeObject(StrategyOptions).Data, nameof(AddStrategyOptions));
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(AddStrategyOptions));
                
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
            StrategyOptions = new PmsStrategyOptions();

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(ClearDataAsync));
            
            return ActionResult.SystemError;
        }
    }
}