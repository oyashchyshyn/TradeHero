using Serilog.Core;
using Serilog.Events;
using TradeHero.Contracts.Services;

namespace TradeHero.Contracts.Logger;

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
                _storeService.Information.WarningCount += 1;
                break;
            case LogEventLevel.Error:
                _storeService.Information.ErrorCount += 1;
                break;
            case LogEventLevel.Fatal:
                _storeService.Information.CriticalCount += 1;
                break;
        }
    }
}