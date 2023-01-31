using TradeHero.Core.Models;
using TradeHero.Core.Models.Github;

namespace TradeHero.Core.Contracts.Services;

public interface IGithubService
{
    event EventHandler<decimal> OnDownloadProgress;
    
    Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync();
    Task<GenericBaseResult<DownloadResponse>> DownloadReleaseAsync(string downloadUri, string filePath,
        CancellationToken cancellationToken = default);
}