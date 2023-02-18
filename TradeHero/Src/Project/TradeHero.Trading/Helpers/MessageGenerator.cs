using TradeHero.Core.Extensions;
using TradeHero.Core.Models.Trading;

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
            $"Shorts market mood: {instanceResult.ShortMarketMoodPercent}%{Environment.NewLine}" +
            $"Longs market mood: {instanceResult.LongsMarketMoodPercent}%{Environment.NewLine}";

        return message;
    }
    
    public static string PositionMessage(SymbolMarketInfo symbolMarketInfo)
    {
        var message =
            $"S: {symbolMarketInfo.FuturesUsdName}{Environment.NewLine}" +
            $"A: {symbolMarketInfo.KlinePocType} | PIW: {symbolMarketInfo.IsPocInWick}{Environment.NewLine}" +
            $"K.D.V.: {symbolMarketInfo.KlineDeltaVolume.ToReadable()} (B: {symbolMarketInfo.KlineBuyVolume.ToReadable()} S: {symbolMarketInfo.KlineSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.V.: {symbolMarketInfo.PocDeltaVolume.ToReadable()} (B: {symbolMarketInfo.PocBuyVolume.ToReadable()} S: {symbolMarketInfo.PocSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.O.: {symbolMarketInfo.PocDeltaTrades} (B: {symbolMarketInfo.PocBuyTrades} S: {symbolMarketInfo.PocSellTrades}){Environment.NewLine}" +
            $"Asks: Q: {symbolMarketInfo.TotalAsks.ToReadable()} (F.L: {symbolMarketInfo.Asks.First().Price.ToReadable()} L.L: {symbolMarketInfo.Asks.Last().Price.ToReadable()}){Environment.NewLine}" +
            $"Bids: Q: {symbolMarketInfo.TotalBids.ToReadable()} (F.L: {symbolMarketInfo.Bids.First().Price.ToReadable()} L.L: {symbolMarketInfo.Bids.Last().Price.ToReadable()}){Environment.NewLine}{Environment.NewLine}";
        
        return message;
    }
}