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
                    _logger.LogError("There is an error during user creation. Please see logs. In {Method}", 
                        nameof(StartAppRunning));
                    
                    return;
                }
            
                if (!File.Exists(appPath))
                {
                    var latestRelease = await _githubService.GetLatestReleaseAsync();
                    if (latestRelease.ActionResult != ActionResult.Success)
                    {
                        _logger.LogError("Cannot get info about application from server! In {Method}", 
                            nameof(StartAppRunning));
                    
                        return;
                    }
                
                    var downloadResult = await _githubService.DownloadReleaseAsync(latestRelease.Data.AppDownloadUri, appPath);

                    if (downloadResult.ActionResult != ActionResult.Success)
                    {
                        _logger.LogError("There is an error during user creation. Please see logs. In {Method}", 
                            nameof(StartAppRunning));
                        
                        return;
                    }
                }

                var arguments = $"{ArgumentKeyConstants.Environment}{_environmentService.GetEnvironmentType()} " +
                        $"{appSettings.Application.RunAppKey}";

                if (_isNeedToUpdatedApp)
                {
                    File.Move(releaseAppPath, appPath, true);

                    arguments += $" {ArgumentKeyConstants.Update}";

                    _isNeedToUpdatedApp = false;
                }
            
                if (_environmentService.GetCurrentOperationSystem() == OperationSystem.Linux)
                {
                    EnvironmentHelper.SetFullPermissionsToFileLinux(appPath);
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
                    
                    
                    _logger.LogError("App process did not started! In {Method}", 
                        nameof(StartAppRunning));
                        
                    return;
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
                    _runningProcess = null;
                    
                    _logger.LogInformation("App is going to be updated. In {Method}", nameof(StartAppRunning));
                    
                    continue;
                }

                _runningProcess.Dispose();
                _runningProcess = null;
                
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