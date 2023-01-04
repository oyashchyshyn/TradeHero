using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services.Models.Environment;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    Dictionary<string, string> CustomArgs { get; }
    EnvironmentSettings GetEnvironmentSettings();
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    string GetDataFolderPath();
    string GetLogsFolderPath();
    string GetDatabaseFolderPath();
    string GetUpdateFolderPath();
    EnvironmentType GetEnvironmentType();
    OperationSystem GetCurrentOperationSystem();
    string GetCurrentApplicationName();
}