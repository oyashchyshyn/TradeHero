using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;

namespace TradeHero.Launcher.Services;

internal class AppService
{
    private readonly ILogger<AppService> _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IGithubService _githubService;
    private readonly IStartupService _startupService;
    
    private Process? _runningProcess;
    private bool _isNeedToUpdatedApp;
    
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
                    throw new ThException("Cannot get info about application from server!");
                }
            
                var downloadResult = await _githubService.DownloadReleaseAsync(latestRelease.Data.AppDownloadUri, appPath);

                if (downloadResult.ActionResult != ActionResult.Success)
                {
                    throw new ThException("Cannot download application from server!");
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
        
            var processStartInfo = new ProcessStartInfo
            {
                FileName = appPath,
                Arguments = arguments,
                UseShellExecute = false
            };
        
            _runningProcess = Process.Start(processStartInfo);
            
            while (_runningProcess is { HasExited: false })
            {
                await Task.Delay(100);
            }

            if (_runningProcess is { ExitCode: (int)AppExitCode.Update })
            {
                _isNeedToUpdatedApp = true;
            
                continue;
            }

            _logger.LogInformation("App finished! In {Method}", nameof(StartAppRunningAsync));
            
            break;
        }
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