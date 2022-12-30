using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Strategy.Models.Instance;

namespace TradeHero.Strategies.Instances.Options;

internal class SpotClusterVolumeOptions : BaseInstanceOptions
{
    private int _volumeAverage;
    public int VolumeAverage
    {
        get => _volumeAverage;
        set => _volumeAverage = value == 0 ? 1 : value;
    }

    public decimal OrderBookDepthPercent { get; set; }
    
    public Market Market => Market.Spot;

    public override string ToString()
    {
        var message =  $"Interval: {Interval} | Volume average: {VolumeAverage} | Order book depth: {OrderBookDepthPercent}% | Side: {Side} | Market: {Market}";
        
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