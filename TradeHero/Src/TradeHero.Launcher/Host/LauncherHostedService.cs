using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;

namespace TradeHero.Launcher.Host;

internal class LauncherHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IGithubService _githubService;
    private readonly IStartupService _startupService;

    private Process? _runningProcess;
    private bool _isNeedToUpdatedApp;
    
    public LauncherHostedService(
        ILoggerFactory loggerFactory, 
        IEnvironmentService environmentService, 
        IGithubService githubService,
        IStartupService startupService
        )
    {
        _logger = loggerFactory.CreateLogger("TradeHero.Launcher");
        _environmentService = environmentService;
        _githubService = githubService;
        _startupService = startupService;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Launcher started. Press Ctrl+C to shut down");
            _logger.LogInformation("Process id: {ProcessId}", _environmentService.GetCurrentProcessId());
            _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());
            _logger.LogInformation("Environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
            _logger.LogInformation("Args: {GetBasePath}", string.Join(", ", _environmentService.GetEnvironmentArgs()));

            if (_environmentService.GetEnvironmentType() == EnvironmentType.Development)
            {
                _logger.LogInformation("Args: {GetBasePath}", string.Join(", ", _environmentService.GetEnvironmentArgs()));   
            }
            
            _githubService.OnDownloadProgress += GithubServiceOnOnDownloadProgress;

            var appSettings = _environmentService.GetAppSettings();
            var dataDirectoryPath = Path.Combine(_environmentService.GetBasePath(), appSettings.Folder.DataFolderName);
            var appPath = Path.Combine(dataDirectoryPath, _environmentService.GetRunningApplicationName());
            var releaseAppPath = Path.Combine(dataDirectoryPath, _environmentService.GetRunningApplicationName());
        
            while (true)
            {
                if (!Directory.Exists(dataDirectoryPath))
                {
                    Directory.CreateDirectory(dataDirectoryPath);
                }

                if (!await _startupService.CheckIsFirstRunAsync())
                {
                    throw new Exception("There is an error during user creation. Please see logs.");
                }
                
                if (!File.Exists(appPath))
                {
                    var latestRelease = await _githubService.GetLatestReleaseAsync();
                    if (latestRelease.ActionResult != ActionResult.Success)
                    {
                        throw new ThException("Cannot get info about application!");
                    }
                    
                    var downloadResult = await _githubService.DownloadReleaseAsync(latestRelease.Data.AppDownloadUri, 
                        appPath, cancellationToken);

                    if (downloadResult.ActionResult != ActionResult.Success)
                    {
                        throw new ThException("Cannot download application!");
                    }
                }

                var arguments =
                    $"{ArgumentKeyConstants.RunApp} {ArgumentKeyConstants.Environment}{_environmentService.GetEnvironmentType()}";

                if (_isNeedToUpdatedApp)
                {
                    File.Move(releaseAppPath, appPath, true);
                    
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
            
            _logger.LogInformation("Launcher stopped");
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StopAsync));
            
            throw;
        }
        
        return Task.CompletedTask;
    }

    #region Private methods

    private static void GithubServiceOnOnDownloadProgress(object? sender, decimal e)
    {
        
    }

    #endregion
}