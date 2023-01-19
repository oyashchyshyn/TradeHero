using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace TradeHero.Launcher.Services;

internal class LauncherHostEnvironment : IHostEnvironment
{
    public LauncherHostEnvironment(
        string environmentName, 
        string applicationName, 
        string contentRootPath, 
        IFileProvider contentRootFileProvider
        )
    {
        EnvironmentName = environmentName;
        ApplicationName = applicationName;
        ContentRootPath = contentRootPath;
        ContentRootFileProvider = contentRootFileProvider;
    }

    public string EnvironmentName { get; set; }
    public string ApplicationName { get; set; }
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
}