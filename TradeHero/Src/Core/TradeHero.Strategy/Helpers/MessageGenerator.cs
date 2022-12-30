using TradeHero.Contracts.Extensions;
using TradeHero.Contracts.Strategy.Models.Instance;

namespace TradeHero.Strategies.Helpers;

internal static class MessageGenerator
{
    public static string KlineResultMessage(InstanceResult clusterVolumeResult)
    {
        var message =
            $"----------------------------------{Environment.NewLine}" +
            $"Interval: {clusterVolumeResult.Interval}{Environment.NewLine}" +
            $"Market: {clusterVolumeResult.Market}{Environment.NewLine}" +
            $"Side: {clusterVolumeResult.Side}{Environment.NewLine}" +
            $"Shorts market mood: {clusterVolumeResult.ShortMarketMoodPercent}%{Environment.NewLine}" +
            $"Longs market mood: {clusterVolumeResult.LongsMarketMoodPercent}%{Environment.NewLine}" +
            $"Total shorts: {clusterVolumeResult.ShortSignals.Count}{Environment.NewLine}" +
            $"Total longs: {clusterVolumeResult.LongSignals.Count}{Environment.NewLine}";

        return message;
    }
    public static string PositionMessage(SymbolMarketInfo symbolMarketInfo)
    {
        var message =
            $"S: {symbolMarketInfo.FuturesUsdName}{Environment.NewLine}" +
            $"S: {symbolMarketInfo.KlinePositionSignal} | A: {symbolMarketInfo.KlineAction}{Environment.NewLine}" +
            $"K.D.V.: {symbolMarketInfo.KlineDeltaVolume.ToReadable()} (B: {symbolMarketInfo.KlineBuyVolume.ToReadable()} S: {symbolMarketInfo.KlineSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.V.: {symbolMarketInfo.PocDeltaVolume.ToReadable()} (B: {symbolMarketInfo.PocBuyVolume.ToReadable()} S: {symbolMarketInfo.PocSellVolume.ToReadable()}){Environment.NewLine}" +
            $"P.D.O.: {symbolMarketInfo.PocDeltaOrders} (B: {symbolMarketInfo.PocBuyOrders} S: {symbolMarketInfo.PocSellOrders}){Environment.NewLine}" +
            $"Asks: Q: {symbolMarketInfo.Asks.Sum(x => x.Quantity).ToReadable()} (F.L: {symbolMarketInfo.Asks.First().Price.ToReadable()} L.L: {symbolMarketInfo.Asks.Last().Price.ToReadable()}){Environment.NewLine}" +
            $"Bids: Q: {symbolMarketInfo.Bids.Sum(x => x.Quantity).ToReadable()} (F.L: {symbolMarketInfo.Bids.First().Price.ToReadable()} L.L: {symbolMarketInfo.Bids.Last().Price.ToReadable()}){Environment.NewLine}{Environment.NewLine}";
        
        return message;
    }
}