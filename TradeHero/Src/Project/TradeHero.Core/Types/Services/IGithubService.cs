using TradeHero.Core.Models;
using TradeHero.Core.Types.Services.Models.Github;

namespace TradeHero.Core.Types.Services;

public interface IGithubService
{
    event EventHandler<decimal> OnDownloadProgress;
    
    Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync();
    Task<GenericBaseResult<DownloadResponse>> DownloadReleaseAsync(string downloadUri, string filePath,
        CancellationToken cancellationToken = default);
}