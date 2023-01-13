namespace TradeHero.Contracts.Services;

public interface IApplicationService
{
    void SetActionsBeforeStopApplication(Func<Task> actionBeforeStopApplication);
    Task StopApplicationAsync();
}