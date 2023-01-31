using Binance.Net.Enums;

namespace TradeHero.Core.Contracts.Services;

public interface IDateTimeService
{
    DateTime GetUtcDateTime();
    DateTime GetLocalDateTime();
    DateTime ConvertToLocalTime(DateTime dateTime);
    DateTime GetNextDateTimeByInterval(DateTime dateTime, KlineInterval interval);
}