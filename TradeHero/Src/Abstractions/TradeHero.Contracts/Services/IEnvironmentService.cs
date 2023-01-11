using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    Dictionary<string, string> CustomArgs { get; }
    string[] GetEnvironmentArgs();
    AppSettings GetAppSettings();
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    EnvironmentType GetEnvironmentType();
    OperationSystem GetCurrentOperationSystem();
    string GetCurrentApplicationName();
    string GetDownloadedApplicationName();
}