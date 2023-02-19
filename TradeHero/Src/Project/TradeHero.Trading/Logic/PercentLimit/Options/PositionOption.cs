using TradeHero.Core.Enums;

namespace TradeHero.Trading.Logic.PercentLimit.Options;

public class PositionOption
{
    public PositionOptionStatus Status { get; set; }
    public KlinePowerSignal KlinePower { get; set; }
    public KlineSignalType KlineSignalType { get; set; }
    public KlineDeltaType PocVolumeDeltaType { get; set; }
    public PocLocation KlinePocLocation { get; set; }
    public PocLevel KlinePocLevel { get; set; }
    public int MinTrades { get; set; }
    public decimal MinQuoteVolume { get; set; }
    public decimal CoefficientOfVolume { get; set; }
    public decimal CoefficientOfPocVolume { get; set; }
    public decimal CoefficientOfOrderLimits { get; set; }
}