using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    string GetBasePath();
    string GetLogsFolderPath();
    string GetDatabaseFolderPath();
    EnvironmentType GetEnvironmentType();
}