using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Services.Models.Update;

namespace TradeHero.Contracts.Services;

public interface IUpdateService
{
    event EventHandler<decimal> OnDownloadProgress;

    Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync();
    Task<GenericBaseResult<DownloadResponse>> UpdateApplicationAsync(ReleaseVersion releaseVersion,
        CancellationToken cancellationToken = default);
}