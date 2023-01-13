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
    private bool _isNeedToFinishProcess;
    private bool _isProcessFinishedWithError;
    
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

    public async Task StartAppRunningAsync()
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

            var _ = Task.Run(() =>
            {
                _runningProcess = Process.Start(processStartInfo);
                
                if (_runningProcess != null)
                {
                    _runningProcess.EnableRaisingEvents = true;   
                    _runningProcess.Exited += RunningProcessOnExited;
                    
                    _logger.LogInformation("App process started! In {Method}", nameof(StartAppRunningAsync));
                }
                else
                {
                    _isProcessFinishedWithError = true;
                    _isNeedToFinishProcess = true;

                    _logger.LogInformation("App process did not started! In {Method}", nameof(StartAppRunningAsync));
                }
            });

            while (!_isNeedToFinishProcess)
            {
                await Task.Delay(100);
            }

            if (_isProcessFinishedWithError)
            {
                throw new Exception("Cannot run bot!");
            }
            
            if (_isNeedToUpdatedApp)
            {
                continue;
            }

            _logger.LogInformation("App finished! In {Method}", nameof(StartAppRunningAsync));
            
            break;
        }
    }

    private void RunningProcessOnExited(object? sender, EventArgs e)
    {
        if (sender is not Process process)
        {
            return;
        }

        _logger.LogInformation("App process exited! Exit code: {ExitCode}. In {Method}", 
            process.ExitCode, nameof(StartAppRunningAsync));
        
        if (process.ExitCode == (int)AppExitCode.Update)
        {
            _isNeedToUpdatedApp = true;
            _logger.LogInformation("App is going to be updated. In {Method}", nameof(StartAppRunningAsync));
        }
        
        _runningProcess?.Dispose();
        _runningProcess = null;
        
        _isNeedToFinishProcess = true;
    }

    public Task StopAppRunningAsync()
    {
        if (_runningProcess == null)
        {
            return Task.CompletedTask;
        }
        
        _runningProcess.Close();
        _runningProcess.Dispose();
        _runningProcess = null;
        
        _logger.LogInformation("App stopped! In {Method}", nameof(StopAppRunningAsync));
        
        return Task.CompletedTask;
    }
}