using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Octokit;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Update;
using TradeHero.Contracts.Settings;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace TradeHero.Core.Services;

internal class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly AppSettings _appSettings;
    private readonly IEnvironmentService _environmentService;
    private readonly ITerminalService _terminalService;
    private readonly IUserRepository _userRepository;

    public UpdateService(
        ILogger<UpdateService> logger, 
        AppSettings appSettings,
        IEnvironmentService environmentService,
        ITerminalService terminalService, 
        IUserRepository userRepository
        )
    {
        _logger = logger;
        _appSettings = appSettings;
        _environmentService = environmentService;
        _terminalService = terminalService;
        _userRepository = userRepository;
    }

    public async Task<GenericBaseResult<ReleaseVersion>> IsNewVersionAvailableAsync()
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue(_appSettings.Github.Repository))
            {
                Credentials = new Credentials(_appSettings.Github.Token)
            };

            var latestReleases = await client.Repository.Release.GetLatest(
                _appSettings.Github.Owner, 
                _appSettings.Github.Repository
            );
            
            if (latestReleases == null)
            {
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
            _logger.LogCritical(exception, "In {Method}", nameof(IsNewVersionAvailableAsync));

            return new GenericBaseResult<ReleaseVersion>(ActionResult.Error);
        }
    }

    public async Task UpdateApplicationAsync(ReleaseVersion releaseVersion)
    {
        try
        {
            _terminalService.ClearConsole();
            
            var currentVersion = _environmentService.GetCurrentApplicationVersion();

            if (currentVersion.CompareTo(releaseVersion.Version) >= 0)
            {
                _logger.LogWarning("Latest release version {ReleaseVersion} is not higher than current version {CurrentVersion}. In {Method}", 
                    releaseVersion.Version.ToString(), currentVersion.ToString(), nameof(IsNewVersionAvailableAsync));

                return;
            }

            var operationSystem = _environmentService.GetCurrentOperationSystem();
            var getCurrentUser = await _userRepository.GetUserAsync();
            var productName = $"TradeHero-{getCurrentUser.TelegramUserId}";
            var productVersion = _environmentService.GetCurrentApplicationVersion().ToString();

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, releaseVersion.DownloadUri);
            
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            request.Headers.Authorization = new AuthenticationHeaderValue("token", _appSettings.Github.Token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(productName, productVersion));

            var response = await httpClient.SendAsync(request);

            var downloadedStream = await response.Content.ReadAsStreamAsync();
            downloadedStream.Position = 0;

            var applicationName = GetApplicationNameByOperationSystem(operationSystem);
            var mainApplicationPath = Path.Combine(_environmentService.GetBasePath(), applicationName);
            var updateFolder = Path.Combine(_environmentService.GetDataFolderPath(), FolderConstants.UpdateFolder);
            var tempFolderApplicationName = Path.Combine(updateFolder, applicationName);

            if (!Directory.Exists(updateFolder))
            {
                Directory.CreateDirectory(updateFolder);
            }
            
            File.Move(mainApplicationPath, tempFolderApplicationName);

            var fileInfo = new FileInfo(mainApplicationPath);
            await using (var fileStream = fileInfo.OpenWrite())
            {
                await downloadedStream.CopyToAsync(fileStream);
            }

            await downloadedStream.DisposeAsync();

            if (!File.Exists(mainApplicationPath))
            {
                _logger.LogWarning(null, "Cannot create new application. In {Method}", 
                    nameof(IsNewVersionAvailableAsync));

                return;
            }

            if (File.Exists(tempFolderApplicationName))
            {
                File.Delete(tempFolderApplicationName);
                
                _logger.LogInformation("{FilePath} deleted. In {Method}", tempFolderApplicationName, 
                    nameof(IsNewVersionAvailableAsync));
            }

            Environment.Exit(0);
            
            Process.Start(mainApplicationPath);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error in {Method}", nameof(UpdateApplicationAsync));
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