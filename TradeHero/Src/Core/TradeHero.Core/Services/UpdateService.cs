using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Octokit;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Base.Models;
using TradeHero.Contracts.Extensions;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Update;
using FileMode = System.IO.FileMode;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace TradeHero.Core.Services;

internal class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IUserRepository _userRepository;

    [DllImport("libc", SetLastError = true)]
    private static extern int chmod(string pathname, int mode);
    
    public event EventHandler<decimal>? OnDownloadProgress;
    
    public bool IsNeedToUpdate { get; private set; }

    public UpdateService(
        ILogger<UpdateService> logger,
        IEnvironmentService environmentService,
        IUserRepository userRepository
        )
    {
        _logger = logger;
        _environmentService = environmentService;
        _userRepository = userRepository;
    }

    public async Task<GenericBaseResult<ReleaseVersion>> GetLatestReleaseAsync()
    {
        try
        {
            var environmentSettings = _environmentService.GetEnvironmentSettings();
            
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
                _logger.LogInformation("There is no releases. In {Method}", nameof(DownloadUpdateAsync));
                
                return new GenericBaseResult<ReleaseVersion>(ActionResult.Null);
            }

            ReleaseAsset appAsset;
            ReleaseAsset updaterAsset;

            switch (_environmentService.GetCurrentOperationSystem())
            {
                case OperationSystem.Windows:
                    appAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_win.exe");
                    updaterAsset = latestReleases.Assets.Single(x => x.Name == "updater_win.exe");
                    break;
                case OperationSystem.Linux:
                    appAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_linux");
                    updaterAsset = latestReleases.Assets.Single(x => x.Name == "updater_linux");
                    break;
                case OperationSystem.Osx:
                    appAsset = latestReleases.Assets.Single(x => x.Name == "trade_hero_osx");
                    updaterAsset = latestReleases.Assets.Single(x => x.Name == "updater_osx");
                    break;
                case OperationSystem.None:
                default:
                    _logger.LogError("Cannot get correct operation system. Current operation system is: {OperationSystem}. In {Method}", 
                        _environmentService.GetCurrentOperationSystem(), nameof(DownloadUpdateAsync));
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
                UpdaterName = updaterAsset.Name,
                UpdaterDownloadUri = updaterAsset.Url
            };

            return new GenericBaseResult<ReleaseVersion>(releaseVersion);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(GetLatestReleaseAsync));

            return new GenericBaseResult<ReleaseVersion>(ActionResult.Error);
        }
    }

    public async Task<GenericBaseResult<DownloadResponse>> DownloadUpdateAsync(ReleaseVersion releaseVersion, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentVersion = _environmentService.GetCurrentApplicationVersion();

            if (currentVersion.CompareTo(releaseVersion.Version) >= 0)
            {
                _logger.LogWarning("Latest release version {ReleaseVersion} is not higher than current version {CurrentVersion}. In {Method}", 
                    releaseVersion.Version.ToString(), currentVersion.ToString(), nameof(DownloadUpdateAsync));

                return new GenericBaseResult<DownloadResponse>(ActionResult.Error);
            }
            
            var activeUser = await _userRepository.GetActiveUserAsync();
            if (activeUser == null)
            {
                _logger.LogError("There is no active user. In {Method}", nameof(DownloadUpdateAsync));

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
            var appFilePath = Path.Combine(_environmentService.GetUpdateFolderPath(), releaseVersion.AppName);
            var updaterFilePath = Path.Combine(_environmentService.GetUpdateFolderPath(), releaseVersion.UpdaterName);

            progressIndicator.ProgressChanged += (sender, progress) =>
            {
                OnDownloadProgress?.Invoke(sender, progress);
            };

            var environmentSettings = _environmentService.GetEnvironmentSettings();
            
            await DownloadFileAsync(releaseVersion.UpdaterDownloadUri, updaterFilePath, productName,
                productVersion, progressIndicator, environmentSettings.Github.Token, cancellationToken);
            
            await DownloadFileAsync(releaseVersion.AppDownloadUri, appFilePath, productName,
                productVersion, progressIndicator, environmentSettings.Github.Token, cancellationToken);

            var downloadResponse = new DownloadResponse
            {
                AppFilePath = appFilePath,
                UpdaterFilePath = updaterFilePath
            };
            
            return new GenericBaseResult<DownloadResponse>(downloadResponse);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error in {Method}", nameof(DownloadUpdateAsync));

            return new GenericBaseResult<DownloadResponse>(ActionResult.SystemError);
        }
    }

    public void SetIsNeedToUpdate()
    {
        IsNeedToUpdate = true;
    }

    public async Task StartUpdateAsync()
    {
        try
        {
            string scriptPath; 
            var operationSystem = _environmentService.GetCurrentOperationSystem();
            var processStartInfo = new ProcessStartInfo();
            
            switch (operationSystem)
            {
                case OperationSystem.Linux:
                {
                    scriptPath = await GenerateUpdateScriptAsync(
                        _environmentService.GetUpdateFolderPath(),
                        "updater_linux.sh"
                    );
                    chmod(scriptPath, 0x1 | 0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x100);
                    processStartInfo.FileName = "sh";
                }
                break;
                case OperationSystem.Windows:
                {
                    scriptPath = await GenerateUpdateScriptAsync(
                        _environmentService.GetUpdateFolderPath(),
                        "updater_win.bat"
                    );
                    processStartInfo.FileName = "cmd";
                }
                break;
                case OperationSystem.None:
                case OperationSystem.Osx:
                default:
                    throw new Exception($"Current operation system is: {operationSystem}.");
            }

            processStartInfo.Arguments = $"{scriptPath} {_environmentService.GetEnvironmentType()} {_environmentService.GetCurrentProcessId()} " +
                 $"{_environmentService.CustomArgs[ArgumentKeyConstants.DownloadApplicationPath]} {_environmentService.GetBasePath()} " +
                 $"{_environmentService.GetCurrentApplicationName()}";
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;

            Process.Start(processStartInfo);

            _logger.LogInformation("Preparing to run process for: {OperationSystem}. Args: {Arguments}. In {Method}", 
                operationSystem, processStartInfo.Arguments, nameof(StartUpdateAsync));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error in {Method}", nameof(StartUpdateAsync));
        }
    }

    #region Private methods

    private static async Task DownloadFileAsync(string downloadUrl, string filePath, string productName, string productVersion, 
        IProgress<decimal>? progressIndicator, string githubToken, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        request.Headers.Authorization = new AuthenticationHeaderValue("token", githubToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(productName, productVersion));

        await using var file = new FileStream(
            filePath, 
            FileMode.Create, FileAccess.Write, FileShare.None
        );

        // Use the custom extension method below to download the data.
        // The passed progress-instance will receive the download status updates.
        await client.DownloadAsync(request, file, progressIndicator, cancellationToken);
    }

    private static async Task<string> GenerateUpdateScriptAsync(string pathToFolder, string fileName)
    {
        await using var manifestResourceStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"TradeHero.Core.Scripts.{fileName}");
        
        if (manifestResourceStream == null)
        {
            throw new Exception($"TradeHero.Core.Scripts.{fileName}");
        }

        var scriptPath = Path.Combine(pathToFolder, fileName);

        await using var fileStream = File.Create(scriptPath);
        manifestResourceStream.Seek(0, SeekOrigin.Begin);
        await manifestResourceStream.CopyToAsync(fileStream);

        return scriptPath;
    }

    #endregion
}