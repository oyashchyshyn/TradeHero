using Serilog.Core;
using Serilog.Events;
using TradeHero.Contracts.Store;

namespace TradeHero.Core.Logger;

internal class StoreEventSink : ILogEventSink
{
    private readonly IStore _store;

    public StoreEventSink(
        IStore store
        )
    {
        _store = store;
    }
    
    public void Emit(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Warning:
                _store.Information.WarningCount += 1;
                break;
            case LogEventLevel.Error:
                _store.Information.ErrorCount += 1;
                break;
            case LogEventLevel.Fatal:
                _store.Information.CriticalCount += 1;
                break;
        }
    }
}