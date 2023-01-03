using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Services;
using TradeHero.Database.Entities;

namespace TradeHero.Database.Worker;

internal class DatabaseFileWorker
{
    private readonly ILogger<DatabaseFileWorker> _logger;
    private readonly IEnvironmentService _environmentService;
    
    public DatabaseFileWorker(
        ILogger<DatabaseFileWorker> logger, 
        IEnvironmentService environmentService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
    }
    
    public IEnumerable<T> GetDataFromFile<T>()
    {
        var fileName = GetPathToDatafile(typeof(T));
        
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new Exception("Wrong file name!");
        }

        var filePath = Path.Combine(
            _environmentService.GetDatabaseFolderPath(),
            fileName
        );

        if (!File.Exists(filePath))
        {
            File.Create(filePath);
            
            _logger.LogInformation("Created file for {FileName}. In {Method}", 
                fileName, nameof(GetDataFromFile));
            
            return new List<T>();
        }

        var stringData = File.ReadAllText(filePath);
            
        if (string.IsNullOrWhiteSpace(stringData))
        {
            return new List<T>();
        }

        var data = JsonConvert.DeserializeObject<T[]>(stringData);
        if (data == null)
        {
            return new List<T>();
        }

        return data;
    }
    
    public void UpdateDataInFile<T>(List<T> objects)
    {
        var fileName = GetPathToDatafile(typeof(T));
        
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new Exception("Wrong file name!");
        }
        
        var path = Path.Combine(
            _environmentService.GetDatabaseFolderPath(),
            fileName
        );

        var jsonSettings = new JsonSerializerSettings();
        jsonSettings.Converters.Add(new StringEnumConverter());
        jsonSettings.Formatting = Formatting.Indented;
        var jsonData = JsonConvert.SerializeObject(objects, jsonSettings);
        
        if (!File.Exists(path))
        {
            File.WriteAllText(path, jsonData);

            return;
        }
        
        File.WriteAllText(path, string.Empty);
        File.WriteAllText(path, jsonData);
    }

    private static string GetPathToDatafile(Type type)
    {
        if (type == typeof(Connection))
        {
            return DatabaseConstants.ConnectionFileName;
        }

        if (type == typeof(Strategy))
        {
            return DatabaseConstants.StrategyFileName;
        }

        return type == typeof(User) ? DatabaseConstants.UserFileName : string.Empty;
    }
}