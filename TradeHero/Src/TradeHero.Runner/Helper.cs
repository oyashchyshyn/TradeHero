using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services.Models.Environment;

namespace TradeHero.Runner;

internal static class Helper
{
    public static EnvironmentType GetEnvironmentType(string[] args)
    {
        if (!args.Any(x => x.StartsWith(ArgumentKeyConstants.Environment)))
        {
            return EnvironmentType.Production;
        }
        
        var argValue = args
            .First(x => x.StartsWith(ArgumentKeyConstants.Environment))
            .Replace(ArgumentKeyConstants.Environment, string.Empty);

        return (EnvironmentType)Enum.Parse(typeof(EnvironmentType), argValue);
    }
    
    public static IConfiguration GenerateConfiguration(string[] args)
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null)
        {
            throw new Exception("Cannot load main assembly");
        }
        
        using var stream = assembly.GetManifestResourceStream("TradeHero.Runner.app.json");
        if (stream == null)
        {
            throw new Exception("Cannot find app.json");
        }

        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .AddCommandLine(args)
            .Build();
    }
    
    public static async Task WriteErrorAsync(Exception exception, string logsFolderPath)
    {            
        var directoryPath = Path.Combine(logsFolderPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
            
        await File.WriteAllTextAsync(
            Path.Combine(directoryPath, "fatal.txt"), exception.ToString()
        );
            
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"FATAL: {exception.Message}");
        Console.ResetColor();
        Console.WriteLine("Press any key for exit...");
        Console.ReadLine();
    }
    
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
        Console.WriteLine("Press any key for exit...");
        Console.ReadLine();
    }
    
    public static void SetCulture()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }

    public static EnvironmentSettings ConvertConfigurationToEnvironmentSettings(IConfiguration configuration)
    {
        return configuration.Get<EnvironmentSettings>() ?? new EnvironmentSettings();
    }

    public static void RunUpdateProcess(Dictionary<string, string> customArgs, OperationSystem operationSystem)
    {
        var arguments =
            $"{customArgs[ArgumentKeyConstants.Environment]} {customArgs[ArgumentKeyConstants.OperationSystem]}" +
            $"{customArgs[ArgumentKeyConstants.DownloadApplicationPath]} {customArgs[ArgumentKeyConstants.ApplicationPath]}" +
            $"{customArgs[ArgumentKeyConstants.BaseApplicationName]}";

        var processStartInfo = new ProcessStartInfo();

        switch (operationSystem)
        {
            case OperationSystem.Linux:
                processStartInfo.FileName = "/bin/bash";
                processStartInfo.Arguments = $"{customArgs[ArgumentKeyConstants.UpdaterPath]} {arguments}";
                processStartInfo.UseShellExecute = false;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                break;
            case OperationSystem.Windows:
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = $"/C start {customArgs[ArgumentKeyConstants.UpdaterPath]} {arguments}";
                processStartInfo.UseShellExecute = false;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                break;   
            }
            case OperationSystem.None:
            case OperationSystem.Osx:
                throw new Exception($"Cannot apply update process to operation system: {operationSystem}");
            default:
                return;
        }

        Process.Start(processStartInfo);
    }
}