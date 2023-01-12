using System.ComponentModel;
using Newtonsoft.Json;
using TradeHero.Menu.Data.Dtos.Base;

namespace TradeHero.Menu.Data.Dtos.TradeLogic;

internal class PercentMoveTradeLogicDto : BaseStrategyDto
{
    [Description("Name for instance. Must be unique. Minimum length 3, Maximum lenght 40.")]
    [JsonProperty("name")]
    public override string Name { get; set; } = string.Empty;
    
    // General
    [Description("Define how much move need to perform for buy order. Available range is 0.0 to 1000.0.")]
    [JsonProperty("price_move_p")]
    public decimal PricePercentMove { get; set; }
}