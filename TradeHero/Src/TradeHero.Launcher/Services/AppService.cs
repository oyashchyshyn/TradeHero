using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;

namespace TradeHero.Launcher.Services;

internal class AppService
{
    private readonly ILogger<AppService> _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IGithubService _githubService;
    private readonly IStartupService _startupService;
    
    private Process? _runningProcess;
    private bool _isNeedToUpdatedApp;
    private bool _isLauncherStopped;

    public AppService(
        ILogger<AppService> logger, 
        IEnvironmentService environmentService, 
        IGithubService githubService, 
        IStartupService startupService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
        _githubService = githubService;
        _startupService = startupService;
    }

    public void StartAppRunning()
    {
        Task.Run(async () =>
        {
            var appPath = Path.Combine(_environmentService.GetBasePath(), _environmentService.GetRunningApplicationName());
            var releaseAppPath = Path.Combine(_environmentService.GetBasePath(), _environmentService.GetReleaseApplicationName());
            var appSettings = _environmentService.GetAppSettings();

            while (true)
            {
                if (!await _startupService.ManageDatabaseDataAsync())
                {
                    throw new Exception("There is an error during user creation. Please see logs.");
                }
            
                if (!File.Exists(appPath))
                {
                    var latestRelease = await _githubService.GetLatestReleaseAsync();
                    if (latestRelease.ActionResult != ActionResult.Success)
                    {
                        throw new Exception("Cannot get info about application from server!");
                    }
                
                    var downloadResult = await _githubService.DownloadReleaseAsync(latestRelease.Data.AppDownloadUri, appPath);

                    if (downloadResult.ActionResult != ActionResult.Success)
                    {
                        throw new Exception("Cannot download application from server!");
                    }

                    if (_environmentService.GetCurrentOperationSystem() == OperationSystem.Linux)
                    {
                        EnvironmentHelper.SetFullPermissionsToFileLinux(appPath);
                    }
                }

                var arguments = $"{ArgumentKeyConstants.Environment}{_environmentService.GetEnvironmentType()} " +
                        $"{appSettings.Application.RunAppKey}";

                if (_isNeedToUpdatedApp)
                {
                    File.Move(releaseAppPath, appPath, true);
                
                    if (_environmentService.GetCurrentOperationSystem() == OperationSystem.Linux)
                    {
                        EnvironmentHelper.SetFullPermissionsToFileLinux(appPath);
                    }
                    
                    arguments += $" {ArgumentKeyConstants.Update}";

                    _isNeedToUpdatedApp = false;
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
                    _logger.LogWarning("App process did not started! In {Method}", nameof(StartAppRunning));
                    
                    throw new Exception("Cannot run bot!");
                }
                
                _logger.LogInformation("App process started! In {Method}", nameof(StartAppRunning));

                while (!_runningProcess.HasExited)
                {
                    await Task.Delay(100);
                }

                if (_isLauncherStopped)
                {
                    break;
                }
                
                _logger.LogInformation("App stopped! In {Method}", nameof(StartAppRunning));
                
                if (_runningProcess.ExitCode == (int)AppExitCode.Update)
                {
                    _isNeedToUpdatedApp = true;
                    _runningProcess.Dispose();
                    
                    _logger.LogInformation("App is going to be updated. In {Method}", nameof(StartAppRunning));
                    
                    continue;
                }

                break;
            }
        });
    }

    public async Task StopAppRunningAsync()
    {
        _isLauncherStopped = true;
        
        if (_runningProcess == null)
        {
            return;
        }
        
        _runningProcess.CloseMainWindow();
        await _runningProcess.WaitForExitAsync();
        
        _runningProcess.Dispose();
        _runningProcess = null;
    }
}