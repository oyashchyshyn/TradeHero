using TradeHero.Core.Enums;
using TradeHero.Core.Types.Settings.AppSettings;

namespace TradeHero.Core.Types.Services;

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
    void SetActionsBeforeStop(Action actionsBeforeStopApplication);
    void StopApplication(AppExitCode? appExitCode = null);
}