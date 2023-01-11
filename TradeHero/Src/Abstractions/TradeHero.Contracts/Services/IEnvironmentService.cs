using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    Dictionary<string, string> CustomArgs { get; }
    string[] GetEnvironmentArgs();
    AppSettings GetEnvironmentSettings();
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    string GetDataFolderPath();
    string GetLogsFolderPath();
    string GetDatabaseFolderPath();
    string GetUpdateFolderPath();
    EnvironmentType GetEnvironmentType();
    int GetCurrentProcessId();
    OperationSystem GetCurrentOperationSystem();
    string GetCurrentApplicationName();
}