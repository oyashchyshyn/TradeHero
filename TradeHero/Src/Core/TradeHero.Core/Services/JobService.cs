using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
using TradeHero.Core.Containers;

namespace TradeHero.Core.Services;

internal class JobService : IJobService
{
    private readonly ILogger<JobService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDateTimeService _dateTimeService;

    private readonly List<JobContainer> _jobs = new();

    public JobService(
        ILoggerFactory loggerFactory, 
        IDateTimeService dateTimeService
        )
    {
        _loggerFactory = loggerFactory;
        _dateTimeService = dateTimeService;

        _logger = loggerFactory.CreateLogger<JobService>();
    }

    public ActionResult StartJob(string key, Func<Task> funcToRun, KlineInterval interval, 
        bool startImmediately = false)
    {
        try
        {
            if (_jobs.Any(x => x.Key == key))
            {
                _logger.LogError("{Key} is exist in jobs collection. In {Method}", 
                    key, nameof(StartJob));
                
                return ActionResult.Error;
            }

            var job = new JobContainer(
                key, 
                funcToRun,
                _loggerFactory.CreateLogger<JobContainer>(),
                _dateTimeService
            );

            job.Create(interval, startImmediately);
            
            _jobs.Add(job);

            _logger.LogInformation("Job with key {Key} registered", key);
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartJob));
            
            return ActionResult.SystemError;
        }
    }
    
    public ActionResult StartJob(string key, Func<Task> funcToRun, TimeSpan delay, 
        bool startImmediately = false)
    {
        try
        {
            if (_jobs.Any(x => x.Key == key))
            {
                _logger.LogError("{Key} is exist in jobs collection. In {Method}", 
                    key, nameof(StartJob));
                
                return ActionResult.Error;
            }

            var job = new JobContainer(
                key, 
                funcToRun,
                _loggerFactory.CreateLogger<JobContainer>(),
                _dateTimeService
            );

            job.Create(delay, startImmediately);
            
            _jobs.Add(job);

            _logger.LogInformation("Job with key {Key} registered", key);
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(StartJob));
            
            return ActionResult.SystemError;
        }
    }

    public ActionResult FinishAllJobs()
    {
        try
        {
            foreach (var jobContainer in _jobs)
            {
                jobContainer.Stop();
            }

            _jobs.Clear();

            _logger.LogInformation("Jobs are finished");
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(FinishAllJobs));
            
            return ActionResult.SystemError;
        }
    }
    
    public ActionResult FinishJobByKey(string key)
    {
        try
        {
            if (_jobs.All(x => x.Key != key))
            {
                _logger.LogError("{Key} does not exist in jobs collection. In {Method}", 
                    key, nameof(StartJob));
                
                return ActionResult.Error;
            }

            var job = _jobs.Single(x => x.Key == key);
            
            job.Stop();

            _jobs.Remove(job);

            _logger.LogInformation("Job with key {Key} finished. In {Method}", 
                key, nameof(FinishJobByKey));
            
            return ActionResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(FinishAllJobs));
            
            return ActionResult.SystemError;
        }
    }
}