using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Binance.Net.Enums;
using Newtonsoft.Json;
using TradeHero.Contracts.Attributes;

namespace TradeHero.EntryPoint.Data.Dtos.Instance;

internal class SpotClusterVolumeOptionsDto
{
    [EnumDescription("Timeframe interval of Kline. Available values are '{0}'.", typeof(KlineInterval))]
    [JsonProperty("interval")]
    public KlineInterval Interval { get; set; }
    
    [EnumDescription("Side ot position to check. Available values are '{0}'.", typeof(PositionSide))]
    [JsonProperty("side")]
    public PositionSide Side { get; set; }
    
    [Description("Enables asynhronious check on results. Available range is 1 to 2000.")]
    [JsonProperty("items_in_task")]
    public int ItemsInTask { get; set; }

    [Description("Telegram channel id. This parameter is optional.")]
    [JsonProperty("tg_channel_id")]
    public long? TelegramChannelId { get; set; }
    
    [Description("Telegram channel name. In order to change telegram name bot need to have permissions. Mimimum length 3, Maximum lenght 125. This parameter is optional.")]
    [JsonProperty("tg_channel_name")]
    public string? TelegramChannelName { get; set; }
    
    [Required]
    [Description("Define is need to send messages from instance. This parameter is optional.")]
    [JsonProperty("tg_allow_send_messages")]
    public bool? TelegramIsNeedToSendMessages { get; set; }
    
    [Description("Average volume. Get results where average volume equal or higher than property value. Available range is 0 to 10000.")]
    [JsonProperty("volume_avg")]
    public int VolumeAverage { get; set; }
    
    [Description("Order book depth percent. Get the depth of order book by percent for bids and asks. Available range is 0.01 to 20.00.")]
    [JsonProperty("order_book_depth_p")]
    public decimal OrderBookDepthPercent { get; set; }
    
    [Description("Quote assets. Assets that are include for search. Iterate assets by comma. Example how to fill values: [USDT,BUSD].")]
    [JsonProperty("qa_include")]
    public List<string> QuoteAssets { get; set; } = new();
    
    [Description("Base assets. Assets that are include for search. Iterate assets by comma. Example how to fill values: [BTC,ETH].")]
    [JsonProperty("ba_include")]
    public List<string> BaseAssets { get; set; } = new();
    
    [Description("Quote assets. Assets that are excluded from search. Iterate assets by comma. Example how to fill values: [BTC,ETH].")]
    [JsonProperty("ba_exclude")]
    public List<string> ExcludeAssets { get; set; } = new();
}