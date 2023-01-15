using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Newtonsoft.Json;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Trading.Models.Instance;

public class SymbolMarketInfo
{
    public bool IsAvailableForTrade { get; set; }

    public string SpotName { get; init; } = string.Empty;
    public string FuturesUsdName { get; init; } = string.Empty;
    public string BaseFuturesUsdAsset { get; init; } = string.Empty;
    public string QuoteAsset { get; init; } = string.Empty;

    public KlinePower Power { get; set; }
    public KlineAction KlineAction { get; set; }
    public PositionSide KlinePositionSide { get; set; }
    public bool IsPocInWick { get; set; }

    // Kline info
    public decimal KlineBuyVolume { get; set; }
    public decimal KlineSellVolume { get; set; }
    public decimal KlineDeltaVolume => KlineBuyVolume - KlineSellVolume;
    public decimal KlineTotalVolume => PocBuyVolume + PocSellVolume;
    public decimal KlineAveragePrice { get; set; }
    public decimal KlineAverageTradeQuoteVolume => KlineAveragePrice * (KlineBuyVolume + KlineSellVolume);
    public decimal KlineVolumeCoefficient => GetCoefficient(KlineBuyVolume, KlineSellVolume);
    
    // Poc volume info
    public decimal PocBuyVolume { get; set; }
    public decimal PocSellVolume { get; set; }
    public decimal PocDeltaVolume => PocBuyVolume - PocSellVolume;
    public decimal PocTotalVolume => PocBuyVolume + PocSellVolume;
    public decimal PocAveragePrice { get; set; }
    public decimal PocAverageTradeQuoteVolume => PocAveragePrice * (PocBuyVolume + PocSellVolume);
    public decimal PocVolumeCoefficient => GetCoefficient(PocBuyVolume, PocSellVolume);

    // Poc volume info
    public int PocBuyTrades { get; set; }
    public int PocSellTrades { get; set; }
    public int PocDeltaTrades => PocBuyTrades - PocSellTrades;
    public int TotalTrades => PocBuyTrades + PocSellTrades;
    public decimal PocTradesCoefficient => GetCoefficient(PocBuyTrades, PocSellTrades);
    
    // OrderBook info
    [JsonIgnore]
    public List<BinanceOrderBookEntry> Asks { get; } = new();
    [JsonIgnore]
    public List<BinanceOrderBookEntry> Bids { get; } = new();
    public decimal TotalAsks => Asks.Sum(x => x.Quantity);
    public decimal TotalBids => Bids.Sum(x => x.Quantity);
    public decimal AsksBidsCoefficient => GetCoefficient(
        Asks.Sum(x => x.Quantity), 
        Bids.Sum(x => x.Quantity)
    );

    #region Private methods

    private static decimal GetCoefficient(decimal buy, decimal sell)
    {
        try
        {
            var delta = buy - sell;
            
            buy = buy <= 0 ? 1 : buy;
            sell = sell <= 0 ? 1 : sell;

            return delta switch
            {
                0 => delta,
                > 0 => buy / sell,
                _ => sell / buy
            };
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            return 0;
        }
    }

    #endregion
}