using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Runner;

internal static class Helper
{
    public static EnvironmentType GetEnvironmentType(string[] args)
    {
        const string environmentArgumentKey = $"--{ArgumentConstants.EnvironmentKey}=";
        
        if (!args.Any(x => x.StartsWith(environmentArgumentKey)))
        {
            return EnvironmentType.Production;
        }
        
        var argValue = args
            .First(x => x.StartsWith(environmentArgumentKey))
            .Replace(environmentArgumentKey, string.Empty);

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
    
    public static async Task WriteExceptionAsync(Exception exception, string baseDirectory)
    {            
        var directoryPath = Path.Combine(baseDirectory, FolderConstants.LogsFolder);
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
    
    public static void SetCulture()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }
}