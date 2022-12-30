using Binance.Net.Enums;

namespace TradeHero.Contracts.Services;

public interface IDateTimeService
{
    DateTime GetUtcDateTime();
    DateTime ConvertToLocalTime(DateTime dateTime);
    DateTime GetNextDateTimeByInterval(DateTime dateTime, KlineInterval interval);
}