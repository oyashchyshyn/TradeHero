using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Octokit;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Update;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Models;
using FileMode = System.IO.FileMode;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace TradeHero.Services.Services;

internal class GithubService : IGithubService
{
    private readonly ILogger<GithubService> _logger;
    private readonly IEnvironmentService _environmentService;

    public event EventHandler<decimal>? OnDownloadProgress;

    public GithubService(
        ILogger<GithubService> logger,
        IEnvironmentService environmentService
    )
    {
        _logger = logger;
        _environmentService = environmentService;
    }

    public async Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync()
    {
        try
        {
            var environmentSettings = _environmentService.GetAppSettings();
            
            var client = new GitHubClient(new ProductHeaderValue(environmentSettings.Github.Owner))
            {
                Credentials = new Credentials(environmentSettings.Github.Token)
            };

            var latestReleases = await client.Repository.Release.GetLatest(
                environmentSettings.Github.Owner, 
                environmentSettings.Github.Repository
            );
            
            if (latestReleases == null)
            {
                _logger.LogInformation("There is no releases. In {Method}", nameof(DownloadReleaseAsync));
                
                return new GenericBaseResult<ReleaseVersion>(ActionResult.Null);
            }

            ReleaseAsset appAsset;
            ReleaseAsset updaterAsset;

            switch (_environmentService.GetCurrentOperationSystem())
            {
                case OperationSystem.Windows:
                    updaterAsset = latestReleases.Assets.Single(x => x.Name == "launcher.exe");
                    appAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_release.exe");
                    break;
                case OperationSystem.Linux:
                    updaterAsset = latestReleases.Assets.Single(x => x.Name == "launcher");
                    appAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_release");
                    break;
                case OperationSystem.None:
                case OperationSystem.Osx:
                default:
                    _logger.LogError("Cannot get correct operation system. Current operation system is: {OperationSystem}. In {Method}", 
                        _environmentService.GetCurrentOperationSystem(), nameof(DownloadReleaseAsync));
                    return new GenericBaseResult<ReleaseVersion>(ActionResult.Null);
            }

            var currentVersion = _environmentService.GetCurrentApplicationVersion();
            var remoteVersion = new Version(latestReleases.Name.Replace("v", string.Empty));
            
            var releaseVersion = new ReleaseVersion
            {
                IsNewAvailable = currentVersion.CompareTo(remoteVersion) == -1,
                Version = remoteVersion,
                AppName = appAsset.Name,
                AppDownloadUri = appAsset.Url,
                LauncherName = updaterAsset.Name,
                LauncherDownloadUri = updaterAsset.Url
            };

            return new GenericBaseResult<ReleaseVersion>(releaseVersion);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetLatestReleaseAsync));

            return new GenericBaseResult<ReleaseVersion>(ActionResult.Error);
        }
    }

    public async Task<GenericBaseResult<DownloadResponse>> DownloadReleaseAsync(string downloadUri, string filePath, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentVersion = _environmentService.GetCurrentApplicationVersion();
            
            var productVersion = _environmentService.GetCurrentApplicationVersion().ToString();
            var progressIndicator = new Progress<decimal>();

            progressIndicator.ProgressChanged += (sender, progress) =>
            {
                OnDownloadProgress?.Invoke(sender, progress);
            };

            var environmentSettings = _environmentService.GetAppSettings();
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);

            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            request.Headers.Authorization = new AuthenticationHeaderValue("token", environmentSettings.Github.Token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TradeHero", productVersion));

            await using var file = new FileStream(
                filePath, 
                FileMode.Create, FileAccess.Write, FileShare.None
            );

            // Use the custom extension method below to download the data.
            // The passed progress-instance will receive the download status updates.
            await client.DownloadAsync(request, file, progressIndicator, cancellationToken);

            var downloadResponse = new DownloadResponse
            {
                FilePath = filePath
            };
            
            return new GenericBaseResult<DownloadResponse>(downloadResponse);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error in {Method}", nameof(DownloadReleaseAsync));

            return new GenericBaseResult<DownloadResponse>(ActionResult.SystemError);
        }
    }
}