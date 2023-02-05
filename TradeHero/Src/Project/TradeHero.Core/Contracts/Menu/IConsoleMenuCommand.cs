namespace TradeHero.Core.Contracts.Menu;

public interface IConsoleMenuCommand
{
    string Id { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
    Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken);
    Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken);
}