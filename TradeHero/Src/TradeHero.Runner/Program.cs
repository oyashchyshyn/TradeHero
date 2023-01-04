using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
using TradeHero.DependencyResolver;
using TradeHero.Runner.Helpers;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Runner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        try
        {
            if (args.Contains("--upt=after-update"))
            {
                Console.WriteLine("Lunched after update!!!");
            }
            
            var environmentType = ArgumentsHelper.GetEnvironmentType(args);
            
            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(baseDirectory)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddConfiguration(ConfigurationHelper.GenerateConfiguration(args));
                })
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddThDependencyCollection();
                })
                .Build();
            
            if (!await host.Services.GetRequiredService<IStartupService>().CheckIsFirstRunAsync())
            {
                throw new Exception("There is an error during user creation. Please see logs.");
            }
            
            var environmentService = host.Services.GetRequiredService<IEnvironmentService>();
            
            await host.StartAsync();

            await host.WaitForShutdownAsync();

            if (environmentService.CustomArgs.ContainsKey("--urd=") && !string.IsNullOrWhiteSpace(environmentService.CustomArgs["--urd="])
                && environmentService.CustomArgs.ContainsKey("--urn=") && !string.IsNullOrWhiteSpace(environmentService.CustomArgs["--urn="]))
            {
                var baseFolderPath = environmentService.CustomArgs["--bfp="];
                var updateFolderPath = environmentService.CustomArgs["--ufp="];
                var mainApplicationName = environmentService.CustomArgs["--man="];
                var downloadedApplicationName = environmentService.CustomArgs["--dan="];
                var workingDirectory = environmentService.CustomArgs["--urd="];
                var fileName = environmentService.CustomArgs["--urn="];

                var processStartInfo = new ProcessStartInfo();
                switch (environmentService.GetCurrentOperationSystem())
                {
                    case OperationSystem.None:
                        break;
                    case OperationSystem.Linux:
                        break;
                    case OperationSystem.Osx:
                        break;
                    case OperationSystem.Windows:
                        processStartInfo.FileName = Path.Combine(workingDirectory, fileName);
                        processStartInfo.Arguments = $"--bfp={baseFolderPath} --ufp={updateFolderPath} --man={mainApplicationName} --dan={downloadedApplicationName}";
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                var process = new Process
                {
                    StartInfo = processStartInfo
                };
                
                process.Start();
            }
        }
        catch (Exception exception)
        {
            await ExceptionHelper.WriteExceptionAsync(exception, baseDirectory);
        }
    }
}