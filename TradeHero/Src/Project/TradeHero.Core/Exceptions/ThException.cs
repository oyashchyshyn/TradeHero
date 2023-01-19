using CryptoExchange.Net.Objects;

namespace TradeHero.Core.Exceptions;

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
        return error?.ToString() ?? string.Empty;
    }

    #endregion
}