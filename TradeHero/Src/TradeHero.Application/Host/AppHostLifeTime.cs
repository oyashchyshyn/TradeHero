using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;

namespace TradeHero.Application.Host;

internal class AppHostLifeTime : IHostLifetime, IDisposable
{
    private readonly ILogger<AppHostLifeTime> _logger;
    private readonly IApplicationService _applicationService;

    private readonly ManualResetEvent _shutdownBlock = new(false);
    
    public AppHostLifeTime(
        ILogger<AppHostLifeTime> logger,
        IApplicationService applicationService
        )
    {
        _logger = logger;
        _applicationService = applicationService;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _shutdownBlock.Set();

        AppDomain.CurrentDomain.UnhandledException -= UnhandledException;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        _logger.LogInformation("Finish disposing. In {Method}", nameof(Dispose));
    }

    #region Private methods

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();

            _logger.LogError("Error in {Method}. Message: {Message}", nameof(UnhandledException), 
                $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}");
        }
        catch (Exception currentException)
        {
            _logger.LogError(currentException, "Error in {Method}", nameof(UnhandledException));
        }
        finally
        {
            _logger.LogError((Exception)e.ExceptionObject, 
                "Error in {Method}. Message: Unhandled exception (AppDomain.CurrentDomain.UnhandledException)", 
                nameof(UnhandledException));
        }
    }
    
    private void OnProcessExit(object? sender, EventArgs e)
    {
        _logger.LogInformation("Exit button is pressed. In {Method}", nameof(OnProcessExit));
        
        _applicationService.StopApplication();

        _shutdownBlock.WaitOne();
        
        Environment.ExitCode = (int)AppExitCode.Success;
    }

    #endregion
}