using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Services;
using TradeHero.Database.Entities;

namespace TradeHero.Database.Worker;

internal class DatabaseFileWorker
{
    private readonly IEnvironmentService _environmentService;
    
    public DatabaseFileWorker(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }
    
    public IEnumerable<T> GetDataFromFile<T>()
    {
        var fileName = GetPathToDatafile(typeof(T));
        
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new Exception("Wrong file name!");
        }
        
        var stringData = File.ReadAllText(
            Path.Combine(
                _environmentService.GetDatabaseFolderPath(), 
                fileName
            )
        );

        if (string.IsNullOrWhiteSpace(stringData))
        {
            throw new Exception($"There is no path file for type {typeof(T)}");
        }

        var data = JsonConvert.DeserializeObject<T[]>(stringData);
        if (data == null || !data.Any())
        {
            throw new Exception($"There is no object data for type {typeof(T)}");
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