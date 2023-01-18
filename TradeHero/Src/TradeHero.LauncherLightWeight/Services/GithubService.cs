using System.Net.Http.Headers;
using Octokit;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.LauncherLightWeight.Helpers;
using TradeHero.LauncherLightWeight.Models;
using FileMode = System.IO.FileMode;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace TradeHero.LauncherLightWeight.Services;

internal class GithubService
{
    private readonly LauncherEnvironment _launcherEnvironment;

    public GithubService(
        LauncherEnvironment launcherEnvironment
        )
    {
        _launcherEnvironment = launcherEnvironment;
    }
    
    public async Task<ReleaseVersion> GetLatestReleaseAsync()
    {
        var appSettings = _launcherEnvironment.GetAppSettings();
        
        var client = new GitHubClient(new ProductHeaderValue(appSettings.Github.Owner))
        {
            Credentials = new Credentials(appSettings.Github.Token)
        };

        var latestReleases = await client.Repository.Release.GetLatest(
            appSettings.Github.Owner, 
            appSettings.Github.Repository
        );
        
        if (latestReleases == null)
        {
            throw new Exception("There is no releases.");
        }

        ReleaseAsset appAsset;
        ReleaseAsset updaterAsset;

        switch (_launcherEnvironment.GetCurrentOperationSystem())
        {
            case OperationSystem.Windows:
                updaterAsset = latestReleases.Assets.Single(x => x.Name == appSettings.Application.WindowsNames.ReleaseLauncher);
                appAsset = latestReleases.Assets.Single(x => x.Name == appSettings.Application.WindowsNames.ReleaseApp);
                break;
            case OperationSystem.Linux:
                updaterAsset = latestReleases.Assets.Single(x => x.Name == appSettings.Application.LinuxNames.ReleaseLauncher);
                appAsset = latestReleases.Assets.Single(x => x.Name == appSettings.Application.LinuxNames.ReleaseApp);
                break;
            case OperationSystem.Osx:
                updaterAsset = latestReleases.Assets.Single(x => x.Name == appSettings.Application.OsxNames.ReleaseLauncher);
                appAsset = latestReleases.Assets.Single(x => x.Name == appSettings.Application.OsxNames.ReleaseApp);
                break;
            case OperationSystem.None:
            default:
                throw new Exception($"Cannot get correct operation system. Current operation system is: {OperationSystem.None}.");
        }

        var currentVersion = _launcherEnvironment.GetCurrentApplicationVersion();
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

        return releaseVersion;
    }

    public async Task DownloadReleaseAsync(string downloadUri, string filePath,
        CancellationToken cancellationToken = default)
    {
        var appSettings = _launcherEnvironment.GetAppSettings();
        
        var productVersion = _launcherEnvironment.GetCurrentApplicationVersion().ToString();
        var progressIndicator = new Progress<decimal>();

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);

        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.Authorization = new AuthenticationHeaderValue("token", appSettings.Github.Token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TradeHero-app", productVersion));

        var file = new FileStream(
            filePath, 
            FileMode.Create, FileAccess.Write, FileShare.None
        );

        // Use the custom extension method below to download the data.
        // The passed progress-instance will receive the download status updates.
        await client.DownloadAsync(request, file, progressIndicator, cancellationToken);

        await file.DisposeAsync();
    }
}