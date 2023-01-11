using TradeHero.Contracts.Services.Models.Update;
using TradeHero.Core.Models;

namespace TradeHero.Contracts.Services;

public interface IGithubService
{
    event EventHandler<decimal> OnDownloadProgress;
    
    Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync();
    Task<GenericBaseResult<DownloadResponse>> DownloadReleaseAsync(string downloadUri, string filePath,
        CancellationToken cancellationToken = default);
}