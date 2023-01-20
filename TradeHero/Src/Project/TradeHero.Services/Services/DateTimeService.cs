using Binance.Net.Enums;
using NCrontab;
using TradeHero.Core.Exceptions;
using TradeHero.Core.Types.Services;

namespace TradeHero.Services.Services;

internal class DateTimeService : IDateTimeService
{
    public DateTime GetUtcDateTime()
    {
        return DateTime.UtcNow;
    }

    public DateTime ConvertToLocalTime(DateTime dateTime)
    {
        return dateTime.ToLocalTime();
    }

    public DateTime GetNextDateTimeByInterval(DateTime dateTime, KlineInterval interval)
    {
        switch (interval)
        {
            case KlineInterval.OneMinute:
                return CrontabSchedule.Parse("*/1 * * * *").GetNextOccurrence(dateTime);
            case KlineInterval.ThreeMinutes:
                return CrontabSchedule.Parse("*/3 * * * *").GetNextOccurrence(dateTime);
            case KlineInterval.FiveMinutes:
                return CrontabSchedule.Parse("*/5 * * * *").GetNextOccurrence(dateTime);
            case KlineInterval.FifteenMinutes:
                return CrontabSchedule.Parse("*/15 * * * *").GetNextOccurrence(dateTime);
            case KlineInterval.ThirtyMinutes:
                return CrontabSchedule.Parse("*/30 * * * *").GetNextOccurrence(dateTime);
            case KlineInterval.OneHour:
                return CrontabSchedule.Parse("0 * * * *").GetNextOccurrence(dateTime);
            case KlineInterval.TwoHour:
                return CrontabSchedule.Parse("0 */2 * * *").GetNextOccurrence(dateTime);
            case KlineInterval.FourHour:
                return CrontabSchedule.Parse("0 */4 * * *").GetNextOccurrence(dateTime);
            case KlineInterval.SixHour:
                return CrontabSchedule.Parse("0 */6 * * *").GetNextOccurrence(dateTime);
            case KlineInterval.EightHour:
                return CrontabSchedule.Parse("0 */8 * * *").GetNextOccurrence(dateTime);
            case KlineInterval.TwelveHour:
                return CrontabSchedule.Parse("0 */12 * * *").GetNextOccurrence(dateTime);
            case KlineInterval.OneDay:
                return CrontabSchedule.Parse("0 0 * * *").GetNextOccurrence(dateTime);
            case KlineInterval.ThreeDay:
                return CrontabSchedule.Parse("0 0 */3 * *").GetNextOccurrence(dateTime);
            case KlineInterval.OneWeek:
                return CrontabSchedule.Parse("0 0 * * 1").GetNextOccurrence(dateTime);
            case KlineInterval.OneMonth:
                return CrontabSchedule.Parse("0 0 1 * *").GetNextOccurrence(dateTime);
            case KlineInterval.OneSecond:
            default:
                throw new ThException("There is no match");
        }
    }
}