using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.Services.Services;

internal class EnvironmentService : IEnvironmentService
{
    private readonly IHostEnvironment _hostingEnvironment;
    private readonly IConfiguration _configuration;

    public EnvironmentService(
        IHostEnvironment hostingEnvironment, 
        IConfiguration configuration
        )
    {
        _hostingEnvironment = hostingEnvironment;
        _configuration = configuration;
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
        return _hostingEnvironment.ContentRootPath;
    }

    public EnvironmentType GetEnvironmentType()
    {
        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), _hostingEnvironment.EnvironmentName);
    }

    public RunnerType GetRunnerType()
    {
        return (RunnerType)Enum.Parse(typeof(RunnerType), _configuration[HostConstants.RunningType] ?? string.Empty);
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
}