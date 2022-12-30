using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Settings;

namespace TradeHero.Core.Services;

internal class InternetConnectionService : IInternetConnectionService
{
    private readonly ILogger<InternetConnectionService> _logger;
    private readonly AppSettings _appSettings;

    private bool _isNeedToStopInternetConnectionChecking;
    private int _currentInternetConnectionAttempts;
    private bool _isInternetConnectionExist;
    private readonly ManualResetEventSlim _manualResetEventSlim = new(true);

    public event EventHandler? OnInternetConnected;
    public event EventHandler? OnInternetDisconnected;

    public InternetConnectionService(
        ILogger<InternetConnectionService> logger,
        AppSettings appSettings
        )
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    public async Task StartInternetConnectionCheckAsync()
    {
        try
        {
            if (_isInternetConnectionExist)
            {
                return;
            }

            await TestConnectionAsync();
        
            CheckInternetConnectionLoop();
            
            _logger.LogInformation("Start internet connection check. In {Method}", 
                nameof(StartInternetConnectionCheckAsync));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(StartInternetConnectionCheckAsync));
        }
    }

    public void StopInternetConnectionChecking()
    {
        try
        {
            _manualResetEventSlim.Dispose();
            
            _isNeedToStopInternetConnectionChecking = true;

            _logger.LogInformation("Finish internet connection check. In {Method}", 
                nameof(StopInternetConnectionChecking));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(StopInternetConnectionChecking));
        }
    }

    public void SetPauseInternetConnectionChecking(bool isNeedPause)
    {
        switch (isNeedPause)
        {
            case true when _manualResetEventSlim.IsSet:
                _manualResetEventSlim.Reset();
                break;
            case false when !_manualResetEventSlim.IsSet:
                _manualResetEventSlim.Set();
                break;
        }
    }
    
    #region Private methods

    private async Task TestConnectionAsync()
    {
        using var ping = new Ping();

        if (await PingRequestAsync(ping, _appSettings.InternetConnection.PingUrl, 
                _appSettings.InternetConnection.PingTimeOutMilliseconds))
        {
            _isInternetConnectionExist = true;

            return;
        }

        _logger.LogWarning("Does not connected to the internet");
    }

    private void CheckInternetConnectionLoop()
    {
        var _ = Task.Run(async () =>
        {
            using var ping = new Ping();

            while (true)
            {
                try
                {
                    if (!_manualResetEventSlim.IsSet)
                    {
                        _manualResetEventSlim.Wait();
                    }
                    
                    if (_isNeedToStopInternetConnectionChecking)
                    {
                        break;
                    }

                    if (await PingRequestAsync(ping, _appSettings.InternetConnection.PingUrl, 
                            _appSettings.InternetConnection.PingTimeOutMilliseconds))
                    {
                        if (_isInternetConnectionExist)
                        {
                            continue;
                        }

                        _logger.LogInformation("Internet connection is connected");

                        _currentInternetConnectionAttempts = 0;
                        _isInternetConnectionExist = true;
                        OnInternetConnected?.Invoke(this, EventArgs.Empty);

                        continue;
                    }

                    if (!_isInternetConnectionExist)
                    {
                        continue;
                    }

                    if (_currentInternetConnectionAttempts >= _appSettings.InternetConnection.ReconnectionAttempts)
                    {
                        _logger.LogWarning("Internet connection is disconnected");

                        _isInternetConnectionExist = false;
                        OnInternetDisconnected?.Invoke(this, EventArgs.Empty);

                        continue;
                    }

                    _currentInternetConnectionAttempts++;
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "In {Method}", 
                        nameof(CheckInternetConnectionLoop));
                }
                finally
                {
                    await Task.Delay(_appSettings.InternetConnection.IterationWaitMilliseconds);
                }
            }
        });
    }

    private async Task<bool> PingRequestAsync(Ping ping, string url, int pingTimeOut)
    {
        PingReply? pingReply = null;
        
        try
        {
            pingReply = await ping.SendPingAsync(url, pingTimeOut);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "In {Method}", nameof(StartInternetConnectionCheckAsync));
        }

        return pingReply?.Status is IPStatus.Success;
    }

    #endregion
}