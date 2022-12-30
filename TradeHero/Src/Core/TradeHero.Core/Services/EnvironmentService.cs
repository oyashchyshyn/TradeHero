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

    public string GetBasePath()
    {
        return _hostingEnvironment.ContentRootPath;
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
}