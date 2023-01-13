namespace TradeHero.Contracts.Services;

public interface IApplicationService
{
    void SetActionsBeforeStopApplication(Action actionsBeforeStopApplication);
    void StopApplication();
}