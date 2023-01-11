using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.Services.Services;

internal class EnvironmentService : IEnvironmentService
{
    private readonly IHostEnvironment _hostingEnvironment;
    private readonly IConfiguration _configuration;

    public Dictionary<string, string> CustomArgs { get; } = new();

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

    public string GetDataFolderPath()
    {
        var environmentSettings = GetAppSettings();
        
        return Path.Combine(GetBasePath(), environmentSettings.Folder.DataFolderName);
    }
    
    public string GetLogsFolderPath()
    {
        var environmentSettings = GetAppSettings();
        
        return Path.Combine(GetBasePath(), environmentSettings.Folder.DataFolderName, environmentSettings.Folder.LogsFolderName);
    }

    public string GetDatabaseFolderPath()
    {
        var environmentSettings = GetAppSettings();
        
        return Path.Combine(GetBasePath(), environmentSettings.Folder.DataFolderName, environmentSettings.Folder.DatabaseFolderName);
    }

    public EnvironmentType GetEnvironmentType()
    {
        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), _hostingEnvironment.EnvironmentName);
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

        var isIos = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        if (isIos)
        {
            return OperationSystem.Osx;
        }

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        return isWindows ? OperationSystem.Windows : OperationSystem.None;
    }

    public string GetCurrentApplicationName()
    {
        var environmentSettings = GetAppSettings();
        
        var applicationName = GetCurrentOperationSystem() switch
        {
            OperationSystem.Windows => environmentSettings.Application.WindowsAppName,
            OperationSystem.Linux => environmentSettings.Application.LinuxAppName,
            OperationSystem.Osx => environmentSettings.Application.LinuxAppName,
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }
    
    public string GetDownloadedApplicationName()
    {
        var environmentSettings = GetAppSettings();
        
        var applicationName = GetCurrentOperationSystem() switch
        {
            OperationSystem.Windows => environmentSettings.Application.DownloadedWindowsAppName,
            OperationSystem.Linux => environmentSettings.Application.DownloadedLinuxAppName,
            OperationSystem.Osx => environmentSettings.Application.DownloadedLinuxAppName,
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }
}