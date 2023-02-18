using Binance.Net.Enums;
using TradeHero.Core.Models.Trading;

namespace TradeHero.Trading.Logic.PercentLimit.Models;

internal class SignalInfo
{
    public string SymbolName { get; }
    public string QuoteName { get; }
    public PositionSide SignalSide { get; }

    public SignalInfo(SymbolMarketInfo symbolMarketInfo, PositionSide positionSide)
    {
        SignalSide = positionSide;
        SymbolName = symbolMarketInfo.FuturesUsdName;
        QuoteName = symbolMarketInfo.QuoteAsset;
    }
}