using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services.Models.Environment;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    EnvironmentSettings GetEnvironmentSettings();
    string? GetEnvironmentValueByKey(string key);
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    string GetDataFolderPath();
    string GetLogsFolderPath();
    string GetDatabaseFolderPath();
    string GetUpdateFolderPath();
    EnvironmentType GetEnvironmentType();
    OperationSystem GetCurrentOperationSystem();
}