using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services.Models.Environment;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    EnvironmentSettings GetEnvironmentSettings();
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    string GetDataFolderPath();
    string GetLogsFolderPath();
    string GetDatabaseFolderPath();
    EnvironmentType GetEnvironmentType();
    OperationSystem GetCurrentOperationSystem();
}