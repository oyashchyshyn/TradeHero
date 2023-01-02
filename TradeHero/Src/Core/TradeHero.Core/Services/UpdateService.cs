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
    public event EventHandler<Exception>? OnUpdateError;

    public async Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync()
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue(_environmentSettings.Github.Repository))
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

            ReleaseAsset releaseAsset;

            switch (_environmentService.GetCurrentOperationSystem())
            {
                case OperationSystem.Windows:
                    releaseAsset = latestReleases.Assets.Single(x => x.Name.Contains("win"));
                    break;
                case OperationSystem.Linux:
                    releaseAsset = latestReleases.Assets.Single(x => x.Name.Contains("linux"));
                    break;
                case OperationSystem.Osx:
                    releaseAsset = latestReleases.Assets.Single(x => x.Name.Contains("osx"));
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
                DownloadUri = releaseAsset.Url
            };

            return new GenericBaseResult<ReleaseVersion>(releaseVersion);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetLatestReleaseAsync));

            return new GenericBaseResult<ReleaseVersion>(ActionResult.Error);
        }
    }

    public async Task UpdateApplicationAsync(ReleaseVersion releaseVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentVersion = _environmentService.GetCurrentApplicationVersion();

            if (currentVersion.CompareTo(releaseVersion.Version) >= 0)
            {
                _logger.LogWarning("Latest release version {ReleaseVersion} is not higher than current version {CurrentVersion}. In {Method}", 
                    releaseVersion.Version.ToString(), currentVersion.ToString(), nameof(UpdateApplicationAsync));

                return;
            }

            var operationSystem = _environmentService.GetCurrentOperationSystem();
            var applicationName = GetApplicationNameByOperationSystem(operationSystem);
            var downloadedApplicationName = $"new_{applicationName}";
            var mainApplicationPath = Path.Combine(_environmentService.GetBasePath(), applicationName);

            if (!Directory.Exists(_environmentService.GetUpdateFolderPath()))
            {
                Directory.CreateDirectory(_environmentService.GetUpdateFolderPath());
            }
            
            var getCurrentUser = await _userRepository.GetUserAsync();
            var productName = $"TradeHero-{getCurrentUser.TelegramUserId}";
            var productVersion = _environmentService.GetCurrentApplicationVersion().ToString();

            var progressIndicator = new Progress<decimal>();

            progressIndicator.ProgressChanged += (sender, progress) =>
            {
                OnDownloadProgress?.Invoke(sender, progress);
            };

            using (var client = new HttpClient()) 
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                using var request = new HttpRequestMessage(HttpMethod.Get, releaseVersion.DownloadUri);

                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                request.Headers.Authorization = new AuthenticationHeaderValue("token", _environmentSettings.Github.Token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue(productName, productVersion));
                
                await using (var file = new FileStream(Path.Combine(_environmentService.GetUpdateFolderPath(), downloadedApplicationName), 
                                 FileMode.Create, FileAccess.Write, FileShare.None)) 
                {
                    // Use the custom extension method below to download the data.
                    // The passed progress-instance will receive the download status updates.
                    await client.DownloadAsync(request, file, progressIndicator, cancellationToken);
                }
            }

            File.Move(mainApplicationPath, Path.Combine(_environmentService.GetUpdateFolderPath(), applicationName));
            
            File.Move(Path.Combine(_environmentService.GetUpdateFolderPath(), downloadedApplicationName), mainApplicationPath);

            if (!File.Exists(mainApplicationPath))
            {
                _logger.LogWarning(null, "Cannot create new application. In {Method}", 
                    nameof(UpdateApplicationAsync));

                throw new Exception("Cannot find download application by main path.");
            }

            if (Directory.Exists(_environmentService.GetUpdateFolderPath()))
            {
                Directory.Delete(_environmentService.GetUpdateFolderPath(), true);
                
                _logger.LogInformation("{DirectoryPath} deleted. In {Method}", _environmentService.GetUpdateFolderPath(), 
                    nameof(UpdateApplicationAsync));
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error in {Method}", nameof(UpdateApplicationAsync));
            
            OnUpdateError?.Invoke(this, exception);
        }
    }

    #region Private nethods

    private static string GetApplicationNameByOperationSystem(OperationSystem operationSystem)
    {
        var applicationName = operationSystem switch
        {
            OperationSystem.Windows => "trade_hero.exe",
            OperationSystem.Linux => "trade_hero",
            OperationSystem.Osx => "trade_hero",
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }

    #endregion
}