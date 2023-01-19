using TradeHero.Core.Enums;

namespace TradeHero.Core.Types.Services;

public interface IApplicationService
{
    void SetActionsBeforeStopApplication(Action actionsBeforeStopApplication);
    void StopApplication(AppExitCode? appExitCode = null);
}