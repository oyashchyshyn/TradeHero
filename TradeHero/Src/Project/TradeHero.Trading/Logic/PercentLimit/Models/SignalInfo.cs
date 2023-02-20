using Binance.Net.Enums;
using TradeHero.Core.Models.Trading;

namespace TradeHero.Trading.Logic.PercentLimit.Models;

internal class SignalInfo
{
    public string SymbolName { get; }
    public string QuoteName { get; }
    public PositionSide SignalSide { get; }

    public SignalInfo(string symbolName, string quoteName, PositionSide positionSide)
    {
        SignalSide = positionSide;
        SymbolName = symbolName;
        QuoteName = quoteName;
    }
}