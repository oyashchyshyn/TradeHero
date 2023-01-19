using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Services;
using TradeHero.Core.Types.Settings.AppSettings;

namespace TradeHero.Services.Services;

internal class EnvironmentService : IEnvironmentService
{
    private readonly ILogger<EnvironmentService> _logger;
    private readonly IConfiguration _configuration;

    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private Action? _actionsBeforeStopApplication;

    public EnvironmentService(
        ILogger<EnvironmentService> logger,
        IConfiguration configuration,
        CancellationTokenSource cancellationTokenSource
        )
    {
        _logger = logger;
        _configuration = configuration;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public string[] GetEnvironmentArgs()
    {
        return Environment.GetCommandLineArgs();
    }

    public AppSettings GetAppSettings()
    {
        return _configuration.Get<AppSettings>() ?? new AppSettings();
    }

    public Version GetCurrentApplicationVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
    }
    
    public string GetBasePath()
    {
        return _configuration[EnvironmentConstants.BasePath] ?? string.Empty;
    }

    public EnvironmentType GetEnvironmentType()
    {
        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), _configuration[EnvironmentConstants.EnvironmentType] ?? string.Empty);
    }

    public RunnerType GetRunnerType()
    {
        return (RunnerType)Enum.Parse(typeof(RunnerType), _configuration[EnvironmentConstants.RunnerType] ?? string.Empty);
    }
    
    public int GetCurrentProcessId()
    {
        return Environment.ProcessId;
    }

    public OperationSystem GetCurrentOperationSystem()
    {
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        if (isLinux)
        {
            return OperationSystem.Linux;
        }
        
        var isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        if (isOsx)
        {
            return OperationSystem.Osx;
        }

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        return isWindows ? OperationSystem.Windows : OperationSystem.None;
    }

    public string GetRunningApplicationName()
    {
        var environmentSettings = GetAppSettings();
        
        var applicationName = GetCurrentOperationSystem() switch
        {
            OperationSystem.Windows => environmentSettings.Application.WindowsNames.App,
            OperationSystem.Linux => environmentSettings.Application.LinuxNames.App,
            OperationSystem.Osx => environmentSettings.Application.OsxNames.App,
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }
    
    public string GetReleaseApplicationName()
    {
        var environmentSettings = GetAppSettings();
        
        var applicationName = GetCurrentOperationSystem() switch
        {
            OperationSystem.Windows => environmentSettings.Application.WindowsNames.ReleaseApp,
            OperationSystem.Linux => environmentSettings.Application.LinuxNames.ReleaseApp,
            OperationSystem.Osx => environmentSettings.Application.OsxNames.App,
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }

    public void SetActionsBeforeStop(Action actionsBeforeStopApplication)
    {
        _actionsBeforeStopApplication = actionsBeforeStopApplication;
    }
    
    public void StopApplication(AppExitCode? appExitCode = null)
    {
        if (_actionsBeforeStopApplication != null)
        {
            _actionsBeforeStopApplication.Invoke();
            
            _logger.LogInformation("Clear resources. In {Method}", nameof(StopApplication));
        }

        if (appExitCode.HasValue)
        {
            Environment.ExitCode = (int)appExitCode.Value;
        }
        
        _cancellationTokenSource.Cancel();
    }
}