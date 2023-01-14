using System.Net;
using TradeHero.Core.Enums;
using TradeHero.Core.Settings.AppSettings;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    string[] GetEnvironmentArgs();
    AppSettings GetAppSettings();
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    int GetCurrentProcessId();
    EnvironmentType GetEnvironmentType();
    RunnerType GetRunnerType();
    OperationSystem GetCurrentOperationSystem();
    string GetRunningApplicationName();
    string GetReleaseApplicationName();
    IPAddress GetLocalIpAddress();
}