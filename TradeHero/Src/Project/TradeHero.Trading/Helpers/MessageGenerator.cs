using TradeHero.Core.Extensions;
using TradeHero.Core.Types.Trading.Models.Instance;

namespace TradeHero.Trading.Helpers;

internal static class MessageGenerator
{
    public static string InstanceResultMessage(InstanceResult instanceResult)
    {
        var message =
            $"----------------------------------{Environment.NewLine}" +
            $"Interval: {instanceResult.Interval}{Environment.NewLine}" +
            $"Market mood: {instanceResult.Market}{Environment.NewLine}" +
            $"Side: {instanceResult.Side}{Environment.NewLine}" +
            $"Signals mood: {instanceResult.SignalsMood}{Environment.NewLine}" +
            $"Shorts market mood: {instanceResult.ShortMarketMoodPercent}%{Environment.NewLine}" +
            $"Longs market mood: {instanceResult.LongsMarketMoodPercent}%{Environment.NewLine}" +
            $"Short signals mood: {instanceResult.ShortSignalMoodPercent}{Environment.NewLine}" +
            $"Long signals mood: {instanceResult.LongsSignalMoodPercent}{Environment.NewLine}" +
            $"Total short signals: {instanceResult.ShortSignalsCount}{Environment.NewLine}" +
            $"Total long signals: {instanceResult.LongSignalsCount}{Environment.NewLine}";

        return message;
    }
    public static string PositionMessage(SymbolMarketInfo symbolMarketInfo)
    {
        var message =
            $"S: {symbolMarketInfo.FuturesUsdName}{Environment.NewLine}" +
            $"S: {symbolMarketInfo.PositionSide} | A: {symbolMarketInfo.KlineAction} | PIW: {symbolMarketInfo.IsPocInWick}{Environment.NewLine}" +
            $"K.D.V.: {symbolMarketInfo.KlineDeltaVolume.ToReadable()} (B: {symbolMarketInfo.KlineBuyVolume.ToReadable()} S: {symbolMarketInfo.KlineSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.V.: {symbolMarketInfo.PocDeltaVolume.ToReadable()} (B: {symbolMarketInfo.PocBuyVolume.ToReadable()} S: {symbolMarketInfo.PocSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.O.: {symbolMarketInfo.PocDeltaTrades} (B: {symbolMarketInfo.PocBuyTrades} S: {symbolMarketInfo.PocSellTrades}){Environment.NewLine}" +
            $"Asks: Q: {symbolMarketInfo.TotalAsks.ToReadable()} (F.L: {symbolMarketInfo.Asks.First().Price.ToReadable()} L.L: {symbolMarketInfo.Asks.Last().Price.ToReadable()}){Environment.NewLine}" +
            $"Bids: Q: {symbolMarketInfo.TotalBids.ToReadable()} (F.L: {symbolMarketInfo.Bids.First().Price.ToReadable()} L.L: {symbolMarketInfo.Bids.Last().Price.ToReadable()}){Environment.NewLine}{Environment.NewLine}";
        
        return message;
    }
}