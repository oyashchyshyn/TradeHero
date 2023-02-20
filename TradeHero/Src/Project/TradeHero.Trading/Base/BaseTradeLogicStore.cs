using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Contracts.Trading;
using TradeHero.Core.Enums;
using TradeHero.Core.Models.Repositories;
using TradeHero.Core.Models.Trading;

namespace TradeHero.Trading.Base;

internal abstract class BaseTradeLogicStore : ITradeLogicStore
{
    protected readonly ILogger Logger;
    protected readonly IJsonService JsonService;

    public SpotMarket Spot { get; private set; } = new();
    public FuturesUsdMarket FuturesUsd { get; private set; } = new();
    public List<Position> Positions { get; } = new();
    public Dictionary<string, ITickerStream?> SymbolTickerStreams { get; } = new();
    public Dictionary<string, decimal> MarketLastPrices { get; } = new();
    public BaseInstanceOptions? InstanceOptions { get; private set; }

    protected BaseTradeLogicStore(
        ILogger logger, 
        IJsonService jsonService
        )
    {
        Logger = logger;
        JsonService = jsonService;
    }

    public abstract ActionResult AddTradeLogicOptions(StrategyDto strategyDto);

    public ActionResult AddInstanceOptions(StrategyDto strategyDto, Type type)
    {
        try
        {
            Logger.LogInformation("Trade options from database: {Data}. In {Method}", 
                strategyDto.TradeLogicJson, nameof(AddTradeLogicOptions));
            
            var strategyOptionsJObject = JsonService.Deserialize<JObject>(strategyDto.InstanceJson);
            if (strategyOptionsJObject.ActionResult != ActionResult.Success)
            {
                Logger.LogError("Cannot deserialize StrategyOptions. In {Method}", nameof(AddTradeLogicOptions));
                
                return ActionResult.Error;
            }
        
            var jObjectStrategyOptionsResult = strategyOptionsJObject.Data.ToObject(type);
            if (jObjectStrategyOptionsResult == null)
            {
                Logger.LogError("JObject of instance is null. In {Method}", nameof(AddTradeLogicOptions));
                
                return ActionResult.Error;
            }
            
            InstanceOptions = (BaseInstanceOptions)jObjectStrategyOptionsResult;

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(AddInstanceOptions));
                
            return ActionResult.SystemError;
        }
    }

    public Task<ActionResult> ClearInstanceOptionsAsync()
    {
        try
        {
            InstanceOptions = null;

            return Task.FromResult(ActionResult.Success);
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(ClearDataAsync));
                
            return Task.FromResult(ActionResult.SystemError);
        }
    }
    
    public virtual async Task<ActionResult> ClearDataAsync()
    {
        try
        {
            await ClearInstanceOptionsAsync();
            
            Positions.Clear();
            MarketLastPrices.Clear();
            SymbolTickerStreams.Clear();
            
            Spot = new SpotMarket();
            FuturesUsd = new FuturesUsdMarket();

            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            Logger.LogCritical(exception, "In {Method}", nameof(ClearDataAsync));
                
            return ActionResult.SystemError;
        }
    }
}