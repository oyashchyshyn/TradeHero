using System.Diagnostics;
using System.Globalization;
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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            
            var processId = Environment.ProcessId;

            Console.WriteLine($"Current process id: {processId}");

            if (args.Contains("--upt=relaunch-app"))
            {
                foreach (var tradeHeroProcess in Process.GetProcesses().Where(x => x.Id != processId && x.ProcessName.Contains("trade_hero")))
                {
                    Console.WriteLine($"{tradeHeroProcess.Id} {tradeHeroProcess.ProcessName}");
                    tradeHeroProcess.Kill(true);
                    tradeHeroProcess.Dispose();
                }
            }

            var counter = Process.GetProcesses().Count(x => x.ProcessName == "trade_hero") > 1;
            if (counter)
            {
                Console.WriteLine(counter);
                Console.WriteLine("TradeHero already running!");
                Console.WriteLine("Press any key to exit.");

                Console.ReadKey();
                
                return;
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

            if (environmentService.CustomArgs.ContainsKey("--upd=") && environmentService.CustomArgs.ContainsKey("--upa="))
            {
                var arguments = environmentService.CustomArgs.Aggregate(string.Empty, (current, customArg) => 
                    current + $"{customArg.Key}{customArg.Value} ");

                var updaterLocation = environmentService.CustomArgs["--upd="];
                var updaterName = environmentService.CustomArgs["--upa="];
                
                var processStartInfo = new ProcessStartInfo();

                switch (environmentService.GetCurrentOperationSystem())
                {
                    case OperationSystem.Linux:
                        processStartInfo.FileName = "/bin/bash";
                        processStartInfo.Arguments = $"{Path.Combine(updaterLocation, updaterName)} {arguments}";
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        break;
                    case OperationSystem.Windows:
                    {
                        processStartInfo.FileName = "cmd.exe";
                        processStartInfo.Arguments = $"/C start {Path.Combine(updaterLocation, updaterName)} {arguments}";
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        break;   
                    }
                    case OperationSystem.None:
                    case OperationSystem.Osx:
                    default:
                        return;
                }

                Process.Start(processStartInfo);
                
                Environment.Exit(0);
            }
        }
        catch (Exception exception)
        {
            await ExceptionHelper.WriteExceptionAsync(exception, baseDirectory);
        }
    }
}