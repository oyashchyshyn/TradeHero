using TradeHero.Core.Enums;
using TradeHero.Core.Models.Client;

namespace TradeHero.Core.Contracts.Client;

public interface IVolumeApi
{
    Task<ThWebCallResult<List<BinanceClusterVolume>>> GetClusterVolumeAsync(string symbol, Market market, 
        DateTime startFrom, DateTime endTo, CancellationToken cancellationToken = default);
}