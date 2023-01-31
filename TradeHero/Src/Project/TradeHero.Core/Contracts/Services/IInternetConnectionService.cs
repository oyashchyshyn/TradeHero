namespace TradeHero.Core.Contracts.Services;

public interface IInternetConnectionService
{
    event EventHandler OnInternetConnected;
    event EventHandler OnInternetDisconnected;

    Task StartInternetConnectionCheckAsync();
    void StopInternetConnectionChecking();
    void SetPauseInternetConnectionChecking(bool isNeedPause);
}