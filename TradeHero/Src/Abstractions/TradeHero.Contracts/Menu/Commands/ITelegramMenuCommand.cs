namespace TradeHero.Contracts.Menu.Commands;

public interface ITelegramMenuCommand
{
    string Id { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
    Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken);
    Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken);
}