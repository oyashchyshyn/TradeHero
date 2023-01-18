using System.Reflection;
using System.Runtime.InteropServices;
using Octokit;
using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.LauncherLightWeight.Helpers;

internal class LauncherEnvironment
{
    private readonly AppSettings _appSettings;
    private readonly EnvironmentType _environmentType;
    private readonly string _basePath;
    
    public LauncherEnvironment(
        AppSettings appSettings, 
        EnvironmentType environmentType,
        string basePath
        )
    {
        _appSettings = appSettings;
        _environmentType = environmentType;
        _basePath = basePath;
    }

    public int GetCurrentProcessId()
    {
        return Environment.ProcessId;
    }
    
    public EnvironmentType GetCurrentEnvironmentType()
    {
        return _environmentType;
    }
    
    public AppSettings GetAppSettings()
    {
        return _appSettings;
    }

    public string GetBasePath()
    {
        return _basePath;
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
    
    public Version GetCurrentApplicationVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
    }
    
    public string GetRunningApplicationName()
    {
        var applicationName = GetCurrentOperationSystem() switch
        {
            OperationSystem.Windows => _appSettings.Application.WindowsNames.App,
            OperationSystem.Linux => _appSettings.Application.LinuxNames.App,
            OperationSystem.Osx => _appSettings.Application.OsxNames.App,
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }
    
    public string GetReleaseApplicationName()
    {
        var applicationName = GetCurrentOperationSystem() switch
        {
            OperationSystem.Windows => _appSettings.Application.WindowsNames.ReleaseApp,
            OperationSystem.Linux => _appSettings.Application.LinuxNames.ReleaseApp,
            OperationSystem.Osx => _appSettings.Application.OsxNames.App,
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }
}