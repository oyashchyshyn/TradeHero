using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Services.Models.Environment;

namespace TradeHero.Core.Services;

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

    public EnvironmentSettings GetEnvironmentSettings()
    {
        return _configuration.Get<EnvironmentSettings>() ?? new EnvironmentSettings();
    }
    
    public string? GetEnvironmentValueByKey(string key)
    {
        return _configuration.GetValue<string>(key);
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
        return Path.Combine(_hostingEnvironment.ContentRootPath, FolderConstants.DataFolder);
    }

    public string GetLogsFolderPath()
    {
        return Path.Combine(_hostingEnvironment.ContentRootPath, FolderConstants.DataFolder, FolderConstants.LogsFolder);
    }
    
    public string GetDatabaseFolderPath()
    {
        return Path.Combine(_hostingEnvironment.ContentRootPath, FolderConstants.DataFolder, FolderConstants.DatabaseFolder);
    }
    
    public string GetUpdateFolderPath()
    {
        return Path.Combine(_hostingEnvironment.ContentRootPath, FolderConstants.DataFolder, FolderConstants.UpdateFolder);
    }

    public EnvironmentType GetEnvironmentType()
    {
        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), _hostingEnvironment.EnvironmentName);
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
    
    public string GetApplicationNameByOperationSystem(OperationSystem operationSystem)
    {
        var applicationName = operationSystem switch
        {
            OperationSystem.Windows => "trade_hero.exe",
            OperationSystem.Linux => "trade_hero",
            OperationSystem.Osx => "trade_hero",
            OperationSystem.None => string.Empty,
            _ => string.Empty
        };

        return applicationName;
    }
}