using TradeHero.Core.Enums;
using TradeHero.Core.Models.Trading;

namespace TradeHero.Trading.Instances.Options;

internal class SpotClusterVolumeOptions : BaseInstanceOptions
{
    private int _volumeAverage;
    public int VolumeAverage
    {
        get => _volumeAverage;
        set => _volumeAverage = value == 0 ? 1 : value;
    }

    public decimal OrderBookDepthPercent { get; set; }

    public override Market Market => Market.Spot;
    public decimal ShortMoodAt { get; set; }
    public decimal LongMoodAt { get; set; }

    public override string ToString()
    {
        var message =  $"Interval: {Interval} | Volume average: {VolumeAverage} | Order book depth: {OrderBookDepthPercent}% " +
                       $"| Side: {Side} | Market: {Market} | Short mood: {ShortMoodAt}% | Long mood: {LongMoodAt}%";
        
        if (QuoteAssets.Any())
        {
            message += $" | QuoteAssets: [{string.Join(",", QuoteAssets)}]";
        }
        
        if (BaseAssets.Any())
        {
            message += $" | BaseAssets: [{string.Join(",", BaseAssets)}]";
        }
        
        if (ExcludeAssets.Any())
        {
            message += $" | ExcludeAssets: [{string.Join(",", ExcludeAssets)}]";
        }

        return message;
    }
}