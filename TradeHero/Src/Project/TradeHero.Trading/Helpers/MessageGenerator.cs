using TradeHero.Contracts.Trading.Models.Instance;
using TradeHero.Core.Extensions;

namespace TradeHero.Trading.Helpers;

internal static class MessageGenerator
{
    public static string InstanceResultMessage(InstanceResult instanceResult)
    {
        var message =
            $"----------------------------------{Environment.NewLine}" +
            $"Interval: {instanceResult.Interval}{Environment.NewLine}" +
            $"Market: {instanceResult.Market}{Environment.NewLine}" +
            $"Side: {instanceResult.Side}{Environment.NewLine}" +
            $"Shorts market mood: {instanceResult.ShortMarketMoodPercent}%{Environment.NewLine}" +
            $"Longs market mood: {instanceResult.LongsMarketMoodPercent}%{Environment.NewLine}" +
            $"Total shorts: {instanceResult.ShortSignals.Count}{Environment.NewLine}" +
            $"Total longs: {instanceResult.LongSignals.Count}{Environment.NewLine}";

        return message;
    }
    public static string PositionMessage(SymbolMarketInfo symbolMarketInfo)
    {
        var message =
            $"S: {symbolMarketInfo.FuturesUsdName}{Environment.NewLine}" +
            $"S: {symbolMarketInfo.KlinePositionSide} | A: {symbolMarketInfo.KlineAction}{Environment.NewLine}" +
            $"K.D.V.: {symbolMarketInfo.KlineDeltaVolume.ToReadable()} (B: {symbolMarketInfo.KlineBuyVolume.ToReadable()} S: {symbolMarketInfo.KlineSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.V.: {symbolMarketInfo.PocDeltaVolume.ToReadable()} (B: {symbolMarketInfo.PocBuyVolume.ToReadable()} S: {symbolMarketInfo.PocSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.O.: {symbolMarketInfo.PocDeltaTrades} (B: {symbolMarketInfo.PocBuyTrades} S: {symbolMarketInfo.PocSellTrades}){Environment.NewLine}" +
            $"Asks: Q: {symbolMarketInfo.Asks.Sum(x => x.Quantity).ToReadable()} (F.L: {symbolMarketInfo.Asks.First().Price.ToReadable()} L.L: {symbolMarketInfo.Asks.Last().Price.ToReadable()}){Environment.NewLine}" +
            $"Bids: Q: {symbolMarketInfo.Bids.Sum(x => x.Quantity).ToReadable()} (F.L: {symbolMarketInfo.Bids.First().Price.ToReadable()} L.L: {symbolMarketInfo.Bids.Last().Price.ToReadable()}){Environment.NewLine}{Environment.NewLine}";
        
        return message;
    }
}