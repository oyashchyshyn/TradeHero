using Binance.Net.Enums;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Services;

public interface IJobService
{
    ActionResult StartJob(string key, Func<Task> funcToRun, KlineInterval interval, bool startImmediately = false);
    ActionResult StartJob(string key, Func<Task> funcToRun, TimeSpan delay, bool startImmediately = false);
    ActionResult FinishJobByKey(string key);
    ActionResult FinishAllJobs();
}