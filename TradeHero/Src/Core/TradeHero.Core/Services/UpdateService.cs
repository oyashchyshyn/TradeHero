using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Octokit;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Extensions;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Environment;
using TradeHero.Contracts.Services.Models.Update;
using FileMode = System.IO.FileMode;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace TradeHero.Core.Services;

internal class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IUserRepository _userRepository;
    
    private readonly EnvironmentSettings _environmentSettings;

    public UpdateService(
        ILogger<UpdateService> logger,
        IEnvironmentService environmentService,
        IUserRepository userRepository
        )
    {
        _logger = logger;
        _environmentService = environmentService;
        _userRepository = userRepository;

        _environmentSettings = _environmentService.GetEnvironmentSettings();
    }
    
    public event EventHandler<decimal>? OnDownloadProgress;

    public async Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync()
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue(_environmentSettings.Github.Owner))
            {
                Credentials = new Credentials(_environmentSettings.Github.Token)
            };

            var latestReleases = await client.Repository.Release.GetLatest(
                _environmentSettings.Github.Owner, 
                _environmentSettings.Github.Repository
            );
            
            if (latestReleases == null)
            {
                _logger.LogInformation("There is no releases. In {Method}", nameof(UpdateApplicationAsync));
                
                return new GenericBaseResult<ReleaseVersion>(ActionResult.Null);
            }

            ReleaseAsset appReleaseAsset;
            ReleaseAsset updaterReleaseAsset;

            switch (_environmentService.GetCurrentOperationSystem())
            {
                case OperationSystem.Windows:
                    appReleaseAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_win.exe");
                    updaterReleaseAsset = latestReleases.Assets.Single(x => x.Name == "updater_win.exe");
                    break;
                case OperationSystem.Linux:
                    appReleaseAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_linux");
                    updaterReleaseAsset = latestReleases.Assets.Single(x => x.Name == "updater_linux");
                    break;
                case OperationSystem.Osx:
                    appReleaseAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_osx");
                    updaterReleaseAsset = latestReleases.Assets.Single(x => x.Name == "updater_osx");
                    break;
                case OperationSystem.None:
                default:
                    _logger.LogError("Cannot get correct operation system. Current operation system is: {OperationSystem}. In {Method}", 
                        _environmentService.GetCurrentOperationSystem(), nameof(UpdateApplicationAsync));
                    return new GenericBaseResult<ReleaseVersion>(ActionResult.Null);
            }

            var currentVersion = _environmentService.GetCurrentApplicationVersion();
            var remoteVersion = new Version(latestReleases.Name.Replace("v", string.Empty));
            
            var releaseVersion = new ReleaseVersion
            {
                IsNewAvailable = currentVersion.CompareTo(remoteVersion) == -1,
                Version = remoteVersion,
                AppName = appReleaseAsset.Name,
                AppDownloadUri = appReleaseAsset.Url,
                UpdaterName = updaterReleaseAsset.Name,
                UpdaterDownloadUri = updaterReleaseAsset.Url
            };

            return new GenericBaseResult<ReleaseVersion>(releaseVersion);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetLatestReleaseAsync));

            return new GenericBaseResult<ReleaseVersion>(ActionResult.Error);
        }
    }

    public async Task<GenericBaseResult<DownloadResponse>> UpdateApplicationAsync(ReleaseVersion releaseVersion, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentVersion = _environmentService.GetCurrentApplicationVersion();

            if (currentVersion.CompareTo(releaseVersion.Version) >= 0)
            {
                _logger.LogWarning("Latest release version {ReleaseVersion} is not higher than current version {CurrentVersion}. In {Method}", 
                    releaseVersion.Version.ToString(), currentVersion.ToString(), nameof(UpdateApplicationAsync));

                return new GenericBaseResult<DownloadResponse>(ActionResult.Error);
            }
            
            var activeUser = await _userRepository.GetActiveUserAsync();
            if (activeUser == null)
            {
                _logger.LogError("There is no active user. In {Method}", nameof(UpdateApplicationAsync));

                return new GenericBaseResult<DownloadResponse>(ActionResult.Error);
            }

            if (!Directory.Exists(_environmentService.GetUpdateFolderPath()))
            {
                Directory.CreateDirectory(_environmentService.GetUpdateFolderPath());
            }
            
            var telegramUserId = activeUser.TelegramUserId;
            var productName = $"TradeHero_{telegramUserId}";
            var productVersion = _environmentService.GetCurrentApplicationVersion().ToString();
            var progressIndicator = new Progress<decimal>();

            await DownloadFileAsync(releaseVersion.UpdaterDownloadUri, releaseVersion.UpdaterName, productName,
                productVersion, null, cancellationToken);
            
            progressIndicator.ProgressChanged += (sender, progress) =>
            {
                OnDownloadProgress?.Invoke(sender, progress);
            };

            await DownloadFileAsync(releaseVersion.AppDownloadUri, releaseVersion.AppName, productName,
                productVersion, progressIndicator, cancellationToken);

            var downloadResponse = new DownloadResponse
            {
                AppFileName = releaseVersion.AppName,
                UpdaterFileName = releaseVersion.UpdaterName,
                UpdateFolderPath = _environmentService.GetUpdateFolderPath()
            };
            
            return new GenericBaseResult<DownloadResponse>(downloadResponse);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error in {Method}", nameof(UpdateApplicationAsync));

            return new GenericBaseResult<DownloadResponse>(ActionResult.SystemError);
        }
    }

    #region Private methods

    private async Task DownloadFileAsync(string downloadUrl, string name, string productName, string productVersion, 
        IProgress<decimal>? progressIndicator, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.Authorization = new AuthenticationHeaderValue("token", _environmentSettings.Github.Token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(productName, productVersion));

        await using var file = new FileStream(
            Path.Combine(_environmentService.GetUpdateFolderPath(), name), 
            FileMode.Create, FileAccess.Write, FileShare.None
        );
            
        // Use the custom extension method below to download the data.
        // The passed progress-instance will receive the download status updates.
        await client.DownloadAsync(request, file, progressIndicator, cancellationToken);
    }

    #endregion
}