using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;
using TradeHero.Launcher.Services;

namespace TradeHero.Launcher.Host;

internal class LauncherHostedService : IHostedService
{
    private readonly ILogger<LauncherHostedService> _logger;
    private readonly EnvironmentService _environmentService;
    private readonly GithubService _githubService;

    private Process? _runningProcess;
    private bool _isNeedToUpdatedApp;
    
    public LauncherHostedService(
        ILogger<LauncherHostedService> logger, 
        EnvironmentService environmentService, 
        GithubService githubService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
        _githubService = githubService;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _githubService.OnDownloadProgress += GithubServiceOnOnDownloadProgress;
            
            var appSettings = _environmentService.GetAppSettings();
            var dataDirectoryPath = Path.Combine(_environmentService.GetBasePath(), appSettings.Folder.DataFolderName);
            var appPath = Path.Combine(dataDirectoryPath, _environmentService.GetCurrentApplicationName());
            var downloadedAppPath = Path.Combine(dataDirectoryPath, _environmentService.GetDownloadedApplicationName());
        
            while (true)
            {
                if (!Directory.Exists(dataDirectoryPath))
                {
                    Directory.CreateDirectory(dataDirectoryPath);
                }

                if (!File.Exists(appPath))
                {
                    var latestRelease = await _githubService.GetLatestReleaseAsync();
                    if (latestRelease.ActionResult != ActionResult.Success)
                    {
                        throw new ThException("Cannot get info about application!");
                    }
                    
                    var downloadResult = await _githubService.DownloadReleaseAsync(latestRelease.Data.DownloadUri, 
                        appPath, cancellationToken);

                    if (downloadResult != ActionResult.Success)
                    {
                        throw new ThException("Cannot download application!");
                    }
                }

                var arguments =
                    $"{ArgumentKeyConstants.RunApp} {ArgumentKeyConstants.Environment}{_environmentService.GetEnvironmentType()}";

                if (_isNeedToUpdatedApp)
                {
                    File.Move(downloadedAppPath, appPath, true);
                    
                    arguments += $" {ArgumentKeyConstants.Update}";
                }
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = arguments,
                    UseShellExecute = false
                };
            
                _runningProcess = Process.Start(processStartInfo);
                if (_runningProcess == null)
                {
                    throw new ThException("Cannot start application!");
                }
            
                await _runningProcess.WaitForExitAsync(cancellationToken);
                
                if (_runningProcess.ExitCode == (int)AppExitCode.Update)
                {
                    _isNeedToUpdatedApp = true;
                    
                    continue;
                }
            
                break;
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartAsync));
            
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _runningProcess?.Close();
            _runningProcess?.Dispose();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StopAsync));
        }
        
        return Task.CompletedTask;
    }

    #region Private methods

    private void GithubServiceOnOnDownloadProgress(object? sender, decimal e)
    {
        
    }

    #endregion
}