using Binance.Net.Enums;

namespace TradeHero.Contracts.Client.Models;

public class BinanceKlineVolatility
{
    public KlineInterval Interval { get; set; }
    public int KlinesCount { get; set; }
    public DateTime StartFrom { get; set; }
    public DateTime EndTo { get; set; }
    public decimal Volatility { get; set; }
}