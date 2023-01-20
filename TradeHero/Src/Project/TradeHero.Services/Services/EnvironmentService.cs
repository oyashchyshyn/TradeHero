using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Types.Services;
using TradeHero.Core.Types.Settings;

namespace TradeHero.Services.Services;

internal class EnvironmentService : IEnvironmentService
{
    private readonly AppSettings _appSettings;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private Action? _actionsBeforeStopApplication;

    public EnvironmentService(
        AppSettings appSettings, 
        CancellationTokenSource cancellationTokenSource
        )
    {
        _appSettings = appSettings;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public string[] GetEnvironmentArgs()
    {
        return Environment.GetCommandLineArgs();
    }

    public AppSettings GetAppSettings()
    {
        return _appSettings;
    }

    public Version GetCurrentApplicationVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
    }
    
    public string GetBasePath()
    {
        return _appSettings.CustomValues[EnvironmentConstants.BasePath];
    }

    public EnvironmentType GetEnvironmentType()
    {
        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), _appSettings.CustomValues[EnvironmentConstants.EnvironmentType]);
    }

    public RunnerType GetRunnerType()
    {
        return (RunnerType)Enum.Parse(typeof(RunnerType), _appSettings.CustomValues[EnvironmentConstants.RunnerType]);
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
        _actionsBeforeStopApplication?.Invoke();

        if (appExitCode.HasValue)
        {
            Environment.ExitCode = (int)appExitCode.Value;
        }
        
        _cancellationTokenSource.Cancel();
    }
}