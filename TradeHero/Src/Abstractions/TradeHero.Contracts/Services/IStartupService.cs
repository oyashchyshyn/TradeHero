namespace TradeHero.Contracts.Services;

public interface IStartupService
{
    Task<bool> CheckIsFirstRunAsync();
}