using Serilog.Core;
using Serilog.Events;
using TradeHero.Core.Contracts.Services;

namespace TradeHero.Core.Logger;

internal class StoreEventSink : ILogEventSink
{
    private readonly IStoreService _storeService;

    public StoreEventSink(
        IStoreService storeService
        )
    {
        _storeService = storeService;
    }
    
    public void Emit(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Warning:
                _storeService.Application.Errors.WarningCount += 1;
                break;
            case LogEventLevel.Error:
                _storeService.Application.Errors.ErrorCount += 1;
                break;
            case LogEventLevel.Fatal:
                _storeService.Application.Errors.CriticalCount += 1;
                break;
        }
    }
}