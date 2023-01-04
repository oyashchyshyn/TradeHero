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
            if (args.Contains("--upt=relaunch-app"))
            {
                var currentProcess = Process.GetCurrentProcess();
                foreach (var tradeHeroProcess in Process.GetProcesses().Where(x => x.Id != currentProcess.Id && x.ProcessName.Contains("trade_hero")))
                {
                    tradeHeroProcess.Kill();
                    tradeHeroProcess.Dispose();
                }
                
                Console.WriteLine("Lunched after update!!!");
            }
            
            //var environmentType = ArgumentsHelper.GetEnvironmentType(args);
            var environmentType = EnvironmentType.Development;
            
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

                File.Move(
                    Path.Combine(baseFolderPath, mainApplicationName), 
                    Path.Combine(updateFolderPath, mainApplicationName)
                );
                
                File.Move(
                    Path.Combine(updateFolderPath, downloadedApplicationName), 
                    Path.Combine(baseFolderPath, mainApplicationName)
                );
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(baseFolderPath, mainApplicationName),
                    Arguments = "--upt=relaunch-app",
                    UseShellExecute = false
                };

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