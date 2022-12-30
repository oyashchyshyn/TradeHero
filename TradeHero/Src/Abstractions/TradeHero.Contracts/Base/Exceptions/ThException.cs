using CryptoExchange.Net.Objects;
using Newtonsoft.Json;

namespace TradeHero.Contracts.Base.Exceptions;

public class ThException : Exception
{
    public ThException(string? message)
        : base(message) 
    { }

    public ThException(Error? error)
        : base(GenerateMessage(error))
    { }

    #region Private methods

    private static string GenerateMessage(Error? error)
    {
        if (error == null)
        {
            return string.Empty;
        }
        
        var message = $"{error.Message}";

        if (error.Code != null)
        {
            message += $" Code: {error.Code}.";
        }

        if (error.Data != null)
        {
            message += $" Object {JsonConvert.SerializeObject(error.Data)}";
        }

        return message;
    }

    #endregion
}