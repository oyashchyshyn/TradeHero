namespace TradeHero.Core.Constants;

public static class ApiConstants
{
    public const int LimitKlineItemsInRequest = 1500; // Max records
    public const int LimitAggregatedTradeHistoryInRequest = 1000; // Max records
    public const int OrderBookItemsWithMinimalWeightAndMaximumCount = 500; // Value 500 is most fits to have good amount of records with minimum weight for endpoint
    public const double AggregatedTradeHistoryMaxDateRageInMilliseconds = 3599999; // 59 minutes and 59 seconds 999 milliseconds
}