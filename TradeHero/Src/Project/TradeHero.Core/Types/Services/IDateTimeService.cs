using Binance.Net.Enums;

namespace TradeHero.Core.Types.Services;

public interface IDateTimeService
{
    DateTime GetUtcDateTime();
    DateTime ConvertToLocalTime(DateTime dateTime);
    DateTime GetNextDateTimeByInterval(DateTime dateTime, KlineInterval interval);
}