using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    string GetDataFolderPath();
    string GetLogsFolderPath();
    string GetDatabaseFolderPath();
    EnvironmentType GetEnvironmentType();
    OperationSystem GetCurrentOperationSystem();
}