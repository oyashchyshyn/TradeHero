using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.LauncherLightWeight.Helpers;

namespace TradeHero.LauncherLightWeight.Services;

internal class LauncherContainer : IDisposable
{
    private readonly ILogger<LauncherContainer> _logger;
    
    private readonly GithubService _githubService;
    private readonly LauncherEnvironment _launcherEnvironment;

    private Process? _runningProcess;
    private bool _isNeedToUpdateApp;

    private readonly ManualResetEvent _shutdownBlock = new(false);

    public LauncherContainer(
        ILogger<LauncherContainer> logger,
        LauncherEnvironment launcherEnvironment
        )
    {
        _logger = logger;
        _launcherEnvironment = launcherEnvironment;

        _githubService = new GithubService(_launcherEnvironment);

        AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;
    }

    public void Start()
    {
        _logger.LogInformation("Started. Press Ctrl+C to shut down");
        _logger.LogInformation("Process id: {ProcessId}", _launcherEnvironment.GetCurrentProcessId());
        _logger.LogInformation("Base path: {GetBasePath}", _launcherEnvironment.GetBasePath());
        _logger.LogInformation("Environment: {GetEnvironmentType}", _launcherEnvironment.GetCurrentEnvironmentType());
        _logger.LogInformation("Runner type: {RunnerType}", RunnerType.Launcher);
        
        Task.Run(async () =>
        {
            var appSettings = _launcherEnvironment.GetAppSettings();
            var appPath = Path.Combine(_launcherEnvironment.GetBasePath(), _launcherEnvironment.GetRunningApplicationName());
            var releaseAppPath = Path.Combine(_launcherEnvironment.GetBasePath(), _launcherEnvironment.GetReleaseApplicationName());

            while (true)
            {
                if (!File.Exists(appPath))
                {
                    Console.WriteLine("Preparing application...");
                    
                    var latestRelease = await _githubService.GetLatestReleaseAsync();

                    await _githubService.DownloadReleaseAsync(latestRelease.AppDownloadUri, appPath);
                }

                var arguments = $"{ArgumentKeyConstants.Environment}{_launcherEnvironment.GetCurrentEnvironmentType()} " +
                        $"{appSettings.Application.RunAppKey}";

                if (_isNeedToUpdateApp)
                {
                    File.Move(releaseAppPath, appPath, true);

                    arguments += $" {ArgumentKeyConstants.Update}";

                    _isNeedToUpdateApp = false;
                }
            
                if (_launcherEnvironment.GetCurrentOperationSystem() == OperationSystem.Linux)
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
                    _logger.LogWarning("App process did not started! In {Method}", nameof(Start));
                    
                    
                    _logger.LogError("App process did not started! In {Method}", 
                        nameof(Start));
                        
                    return;
                }
                
                _logger.LogInformation("App process started! In {Method}", nameof(Start));

                await _runningProcess.WaitForExitAsync();

                _logger.LogInformation("App stopped. Exit code: {ExitCode}. In {Method}", 
                    _runningProcess.ExitCode, nameof(Start));
                
                if (_runningProcess.ExitCode == (int)AppExitCode.Update)
                {
                    _isNeedToUpdateApp = true;
                    
                    _runningProcess?.Dispose();
                    _runningProcess = null;
                    
                    _logger.LogInformation("App is going to be updated. In {Method}", nameof(Start));
                    
                    continue;
                }

                _runningProcess?.Dispose();
                _runningProcess = null;

                break;
            }
        });
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.UnhandledException -= UnhandledException;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        Console.CancelKeyPress -= OnCancelKeyPress;
        
        _shutdownBlock.Set();
        
        _logger.LogInformation("Finish disposing. In {Method}", nameof(Dispose));
    }

    #region Private methods

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger.LogInformation("Ctrl + C is pressed. In {Method}", nameof(OnCancelKeyPress));
        
        e.Cancel = true;

        _shutdownBlock.WaitOne();
        
        Environment.ExitCode = (int)AppExitCode.Success;
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _logger.LogInformation("Exit button is pressed. In {Method}", nameof(OnCancelKeyPress));

        _shutdownBlock.WaitOne();
        
        Environment.ExitCode = (int)AppExitCode.Success;
    }

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();

            _logger.LogError("Error in {Method}. Message: {Message}", nameof(UnhandledException), 
                $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}");
        }
        catch (Exception currentException)
        {
            _logger.LogError(currentException, "Error in {Method}", nameof(UnhandledException));
        }
        finally
        {
            _logger.LogError((Exception)e.ExceptionObject, 
                "Error in {Method}. Message: Unhandled exception (AppDomain.CurrentDomain.UnhandledException)", 
                nameof(UnhandledException));
        }
    }

    #endregion
}