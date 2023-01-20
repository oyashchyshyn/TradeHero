using TradeHero.Core.Enums;
using TradeHero.Core.Types.Client.Models;
using TradeHero.Core.Types.Client.Models.Response;

namespace TradeHero.Core.Types.Client.CustomApi;

public interface IVolumeApi
{
    Task<ThWebCallResult<List<BinanceClusterVolume>>> GetClusterVolumeAsync(string symbol, Market market, 
        DateTime startFrom, DateTime endTo, CancellationToken cancellationToken = default);
}