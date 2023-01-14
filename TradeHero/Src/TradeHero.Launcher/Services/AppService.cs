﻿using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Sockets;
using TradeHero.Contracts.Sockets.Args;
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
    private readonly IApplicationService _applicationService;
    private readonly IServerSocket _serverSocket;
    
    private Process? _runningProcess;
    private bool _isNeedToUpdatedApp;
    private bool _isLauncherStopped;

    public AppService(
        ILogger<AppService> logger, 
        IEnvironmentService environmentService, 
        IGithubService githubService, 
        IStartupService startupService, 
        IApplicationService applicationService, 
        IServerSocket serverSocket
        )
    {
        _logger = logger;
        _environmentService = environmentService;
        _githubService = githubService;
        _startupService = startupService;
        _applicationService = applicationService;
        _serverSocket = serverSocket;
    }

    public void StartAppRunning()
    {
        Task.Run(async () =>
        {
            _serverSocket.OnReceiveMessageFromClient += ServerSocketOnOnReceiveMessageFromClient;
            
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

                await _runningProcess.WaitForExitAsync();

                _logger.LogInformation("App stopped. Exit code: {ExitCode}. In {Method}", 
                    _runningProcess.ExitCode, nameof(StartAppRunning));
                
                if (_runningProcess.ExitCode == (int)AppExitCode.Update)
                {
                    _runningProcess?.Dispose();
                    _runningProcess = null;
                    
                    _logger.LogInformation("App is going to be updated. In {Method}", nameof(StartAppRunning));
                    
                    continue;
                }

                _runningProcess?.Dispose();
                _runningProcess = null;
                
                if (_isLauncherStopped)
                {
                    break;
                }

                _applicationService.StopApplication();
                
                break;
            }
        });
    }

    public void StopAppRunning()
    {
        _isLauncherStopped = true;
    }

    #region Private methods

    private void ServerSocketOnOnReceiveMessageFromClient(object? sender, SocketMessageArgs socketArgs)
    {
        if (!Enum.TryParse(socketArgs.Message, out ApplicationCommands currentCommand))
        {
            return;
        }
        
        if (currentCommand == ApplicationCommands.Update)
        {
            _isNeedToUpdatedApp = true;
        }
    }

    #endregion
}