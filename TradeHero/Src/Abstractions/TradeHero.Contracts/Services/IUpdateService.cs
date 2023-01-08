using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Services.Models.Update;

namespace TradeHero.Contracts.Services;

public interface IUpdateService
{
    event EventHandler<decimal> OnDownloadProgress;

    bool IsNeedToUpdate { get; }

    Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync();
    Task<GenericBaseResult<DownloadResponse>> DownloadUpdateAsync(ReleaseVersion releaseVersion, CancellationToken cancellationToken = default);
    void SetIsNeedToUpdate();
    Task StartUpdateAsync();
}