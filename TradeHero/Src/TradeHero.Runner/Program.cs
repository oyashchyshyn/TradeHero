﻿using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeHero.Contracts.Services;
using TradeHero.Core.Constants;
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
                    serviceCollection.AddThDependencyCollection();
                })
                .Build();
            
            if (!await host.Services.GetRequiredService<IStartupService>().CheckIsFirstRunAsync())
            {
                Helper.WriteError("There is an error during user creation. Please see logs.");

                return;
            }
            
            var updateService = host.Services.GetRequiredService<IUpdateService>();
            
            await host.RunAsync();

            if (updateService.IsNeedToUpdate)
            {
                await updateService.StartUpdateAsync();
            }
            
            Environment.Exit(0);
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