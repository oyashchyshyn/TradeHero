using Binance.Net.Enums;

namespace TradeHero.Contracts.StrategyRunner.Models;

public class Position
{
    public string Name { get; init; } = string.Empty;
    public string BaseAsset { get; init; } = string.Empty;
    public string QuoteAsset { get; init; } = string.Empty;
    public PositionSide PositionSide { get; init; }
    public decimal EntryPrice { get; set; }
    public decimal TotalQuantity { get; set; }
    public int Leverage { get; set; } = 1;
    public decimal InitialMargin => EntryPrice * TotalQuantity * (1 / (decimal)Leverage);
    public DateTime LastUpdateTime { get; set; }

    public override string ToString()
    {
        return $"[{Name} | {PositionSide} | P:{EntryPrice} | Q:{TotalQuantity} | L:x{Leverage} | M:{InitialMargin}$]";
    }
}