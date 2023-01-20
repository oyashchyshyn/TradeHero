using CryptoExchange.Net.Objects;

namespace TradeHero.Core.Types.Client.Models.Response;

public class ThError : Error
{
    public ThError(int? code, string message, object? data) 
        : base(code, message, data)
    { }

    public ThError(Exception exception) 
        : base(null, exception.Message, null) 
    { }
}