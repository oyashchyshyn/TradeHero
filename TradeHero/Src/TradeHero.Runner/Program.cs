using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Services;
using TradeHero.DependencyResolver;
using HostApp = Microsoft.Extensions.Hosting.Host;

namespace TradeHero.Runner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = Helper.GenerateConfiguration(args);
        var environmentSettings = Helper.ConvertConfigurationToEnvironmentSettings(configuration);

        try
        {
            Helper.SetCulture();
            
            if (args.Contains(ArgumentKeyConstants.Update))
            {
                var processes = Process.GetProcesses().Where(x =>
                    x.Id != Environment.ProcessId &&
                    x.ProcessName.Contains(environmentSettings.Application.BaseAppName));
                
                foreach (var tradeHeroProcess in processes)
                {
                    tradeHeroProcess.Kill(true);
                    tradeHeroProcess.Dispose();
                }
            }
            
            if (Process.GetProcesses().Count(x => x.ProcessName == environmentSettings.Application.BaseAppName) > 1)
            {
                Helper.WriteError("Bot already running!");
                
                return;
            }
            
            var environmentType = Helper.GetEnvironmentType(args);

            var host = HostApp.CreateDefaultBuilder(args)
                .UseEnvironment(environmentType.ToString())
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddOptions<HostOptions>()
                        .Configure(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(15));
                    
                    serviceCollection.AddThDependencyCollection();
                })
                .Build();
            
            if (!await host.Services.GetRequiredService<IStartupService>().CheckIsFirstRunAsync())
            {
                Helper.WriteError("There is an error during user creation. Please see logs.");

                return;
            }
            
            var environmentService = host.Services.GetRequiredService<IEnvironmentService>();
            
            await host.RunAsync();

            if (environmentService.CustomArgs.ContainsKey(ArgumentKeyConstants.UpdaterPath))
            {
                Helper.RunUpdateProcess(environmentService.CustomArgs, environmentService.GetCurrentOperationSystem());
                
                Environment.Exit(0);
            }
        }
        catch (Exception exception)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                environmentSettings.Folder.DataFolderName, environmentSettings.Folder.LogsFolderName);
            
            await Helper.WriteErrorAsync(exception, path);
            
            Environment.Exit(-1);
        }
    }
}