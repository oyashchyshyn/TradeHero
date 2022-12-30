using Binance.Net.Enums;
using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Contracts.Services;

public interface IJobService
{
    ActionResult StartJob(string key, Func<Task> funcToRun, KlineInterval interval, bool startImmediately = false);
    ActionResult StartJob(string key, Func<Task> funcToRun, TimeSpan delay, bool startImmediately = false);
    ActionResult FinishJobByKey(string key);
    ActionResult FinishAllJobs();
}