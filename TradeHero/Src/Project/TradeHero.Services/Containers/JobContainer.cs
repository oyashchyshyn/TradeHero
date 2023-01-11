using System.Diagnostics;
using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using SystemDateTime = System.DateTime;

namespace TradeHero.Services.Containers;

internal class JobContainer
{
    private readonly ILogger<JobContainer> _logger;
    private readonly IDateTimeService _dateTimeService;

    private readonly Stopwatch _stopwatch = new();
    private bool _isNeedToStopJob;
    
    private readonly Func<Task> _funcToRun;
    private SystemDateTime _startAt;

    public string Key { get; }

    public JobContainer(
        string key,
        Func<Task> funcToRun,
        ILogger<JobContainer> logger,
        IDateTimeService dateTimeService
        )
    {
        Key = key;
        _funcToRun = funcToRun;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public void Create(TimeSpan delay, bool startImmediately = false)
    {
        _startAt = startImmediately 
            ? _dateTimeService.GetUtcDateTime().AddMinutes(-1) 
            : _dateTimeService.GetUtcDateTime().AddMilliseconds(delay.TotalMilliseconds);

        _logger.LogInformation("Create Job with Key: {Key}. Start will be at {Start}", 
            Key, _startAt.ToString("dd.MM.yyyy HH:mm:ss.fff UTC"));

        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    if (_isNeedToStopJob)
                    {
                        break;
                    }

                    if (_dateTimeService.GetUtcDateTime() >= _startAt)
                    {
                        _logger.LogInformation("Run Job with Key: {Key}", Key);

                        _stopwatch.Start();
                            
                        await _funcToRun();
                            
                        _startAt = _dateTimeService.GetUtcDateTime().AddMilliseconds(delay.TotalMilliseconds);
                            
                        _logger.LogInformation("Run Job with Key: {Key}. Run finished. Elapsed: {Elapsed}. Next run will be at {Start}", 
                            Key, _stopwatch.Elapsed, _startAt.ToString("dd.MM.yyyy HH:mm:ss.fff UTC"));
                            
                        _stopwatch.Stop();
                        _stopwatch.Reset();
                    }

                    await Task.Delay(100);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error in job task with key: {Key}", Key);
            }
        });
    }

    public void Create(KlineInterval interval, bool startImmediately = false)
    {
        _startAt = startImmediately 
            ? _dateTimeService.GetUtcDateTime().AddMinutes(-1) 
            : _dateTimeService.GetNextDateTimeByInterval(_dateTimeService.GetUtcDateTime(), interval);

        _logger.LogInformation("Create Job with Key: {Key}. Start will be at {Start}", 
            Key, _startAt.ToString("dd.MM.yyyy HH:mm:ss.fff UTC"));

        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    if (_isNeedToStopJob)
                    {
                        break;
                    }
                
                    if (_dateTimeService.GetUtcDateTime() >= _startAt)
                    {
                        _logger.LogInformation("Run Job with Key: {Key}", Key);

                        _stopwatch.Start();
                            
                        await _funcToRun();
                         
                        _startAt = _dateTimeService.GetNextDateTimeByInterval(
                            _dateTimeService.GetUtcDateTime(), 
                            interval
                        );
                            
                        _logger.LogInformation("Run Job with Key: {Key}. Run finished. Elapsed: {Elapsed}. Next run will be at {Start}", 
                            Key, _stopwatch.Elapsed, _startAt.ToString("dd.MM.yyyy HH:mm:ss.fff UTC"));

                        _stopwatch.Stop();
                        _stopwatch.Reset();
                    }
            
                    await Task.Delay(100);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error in job task with key: {Key}", Key);
            }
        });
    }
    
    public void Stop()
    {
        _isNeedToStopJob = true;

        _logger.LogInformation("Stopping Job with Key: {Key}", Key);
    }
}