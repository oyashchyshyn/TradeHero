namespace TradeHero.Contracts.Menu;

public interface IMenuCommand
{
    string Id { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
    Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken);
    Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken);
}