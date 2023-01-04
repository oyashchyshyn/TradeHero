using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Base.Constants;
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
            if (args.Contains("--upt=re-lunch"))
            {
                var dictionary = args.Select(arg => arg.Split("="))
                    .ToDictionary(split => $"{split[0]}=", split => split[1]);

                var baseFolderPath = dictionary["--bfp="];
                var updateFolderPath = dictionary["--ufp="];
                var mainApplicationName = dictionary["--man="];
                var downloadedApplicationName = dictionary["--dan="];
                
                File.Move(
                    Path.Combine(baseFolderPath, $"old_{mainApplicationName}"), 
                    Path.Combine(updateFolderPath, $"old_{mainApplicationName}")
                );
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
            
            var environment = host.Services.GetRequiredService<IEnvironmentService>();
            
            await host.StartAsync();

            await host.WaitForShutdownAsync();

            if (environment.CustomArgs.ContainsKey("--upt="))
            {
                var baseFolderPath = environment.CustomArgs["--bfp="];
                var updateFolderPath = environment.CustomArgs["--ufp="];
                var mainApplicationName = environment.CustomArgs["--man="];
                var downloadedApplicationName = environment.CustomArgs["--dan="];
                
                File.Move(
                    Path.Combine(updateFolderPath, downloadedApplicationName), 
                    Path.Combine(baseFolderPath, mainApplicationName)
                );

                var process = new Process();
                process.StartInfo.FileName = Path.Combine(baseFolderPath, downloadedApplicationName);
                process.StartInfo.Arguments = $"--upt=re-lunch --bfp={baseFolderPath} --ufp={updateFolderPath} --man={mainApplicationName} --dan={downloadedApplicationName}";
                process.Start();
            }
        }
        catch (Exception exception)
        {
            await ExceptionHelper.WriteExceptionAsync(exception, baseDirectory);
        }
    }
}