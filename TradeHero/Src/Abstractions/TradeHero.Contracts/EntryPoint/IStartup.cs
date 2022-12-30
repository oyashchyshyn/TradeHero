namespace TradeHero.Contracts.EntryPoint;

public interface IStartup
{
    Task StartAsync();
    Task EndAsync();
}