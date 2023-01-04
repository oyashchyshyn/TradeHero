using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            
            var environment = host.Services.GetRequiredService<IEnvironmentService>();
            
            await host.StartAsync();

            await host.WaitForShutdownAsync();

            if (environment.CustomArgs.ContainsKey("--upd=") && !string.IsNullOrWhiteSpace(environment.CustomArgs["--upd="]))
            {
                var baseFolderPath = environment.CustomArgs["--bfp="];
                var updateFolderPath = environment.CustomArgs["--ufp="];
                var mainApplicationName = environment.CustomArgs["--man="];
                var downloadedApplicationName = environment.CustomArgs["--dan="];

                var process = new Process();
                process.StartInfo.FileName = environment.CustomArgs["--upd="];
                process.StartInfo.Arguments = $"--bfp={baseFolderPath} --ufp={updateFolderPath} --man={mainApplicationName} --dan={downloadedApplicationName}";
                process.Start();
            }
        }
        catch (Exception exception)
        {
            await ExceptionHelper.WriteExceptionAsync(exception, baseDirectory);
        }
    }
}