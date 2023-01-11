using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;

namespace TradeHero.Launcher.Host;

internal class LauncherHostedService : IHostedService
{
    private readonly ILogger<LauncherHostedService> _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IGithubService _githubService;

    private Process? _runningProcess;
    private bool _isNeedToUpdatedApp;
    
    public LauncherHostedService(
        ILogger<LauncherHostedService> logger, 
        IEnvironmentService environmentService, 
        IGithubService githubService
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
                        throw new Exception();
                    }
        
                    var downloadResult = await _githubService.DownloadReleaseAsync(latestRelease.Data.AppDownloadUri, 
                        appPath, cancellationToken);

                    if (downloadResult.ActionResult != ActionResult.Success)
                    {
                        throw new Exception();
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
                    throw new Exception();
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
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _runningProcess?.Close();
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StopAsync));
        }
        
        return Task.CompletedTask;
    }
}