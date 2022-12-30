using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Client.Models;
using TradeHero.Contracts.Client.Models.Response;

namespace TradeHero.Contracts.Client.CustomApi;

public interface IVolumeApi
{
    Task<ThWebCallResult<List<BinanceClusterVolume>>> GetClusterVolumeAsync(string symbol, Market market, DateTime startFrom, 
        DateTime endTo, int step, CancellationToken cancellationToken = default);
}