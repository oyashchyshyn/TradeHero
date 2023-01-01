using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;

namespace TradeHero.Core.Services;

internal class EnvironmentService : IEnvironmentService
{
    private readonly IHostEnvironment _hostingEnvironment;

    public EnvironmentService(IHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }

    public Version GetCurrentApplicationVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
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
}