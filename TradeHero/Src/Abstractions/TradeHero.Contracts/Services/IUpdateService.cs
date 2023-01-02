using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Services.Models.Update;

namespace TradeHero.Contracts.Services;

public interface IUpdateService
{
    event EventHandler<decimal> OnDownloadProgress;
    event EventHandler<Exception> OnUpdateError;
    
    Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync();
    Task UpdateApplicationAsync(ReleaseVersion releaseVersion, CancellationToken cancellationToken = default);
}