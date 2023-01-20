using Binance.Net.Enums;

namespace TradeHero.Core.Types.Services.Models.Calculator;

public struct CalculatedOrderQuantity
{
    public PositionSide Side { get; init; }
    public decimal MinRoePercent { get; init; }
    public decimal LastPrice { get; init; }
    public decimal EntryPrice { get; init; }
    public decimal TotalQuantity { get; init; }
    public decimal MinNotional { get; init; }
    public decimal MinOrderSize { get; set; }
    public decimal Leverage { get; set; }
}