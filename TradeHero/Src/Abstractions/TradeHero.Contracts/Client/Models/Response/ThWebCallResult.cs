using System.Net;
using CryptoExchange.Net.Objects;

namespace TradeHero.Contracts.Client.Models.Response;

public class ThWebCallResult<T> : WebCallResult<T>
{
    public ThWebCallResult(T data)
        : base(null, null, null, null, null, null, null, null, data, null)
    {
        
    }
    
    public ThWebCallResult(
        HttpStatusCode? code, 
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? responseHeaders, 
        TimeSpan? responseTime, 
        string? originalData, 
        string? requestUrl, 
        string? requestBody, 
        HttpMethod? requestMethod, 
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders, 
        T data, 
        Error? error
        ) : base(code, responseHeaders, responseTime, originalData, requestUrl, requestBody, requestMethod, requestHeaders, data, error)
    {
    }

    public ThWebCallResult(Error? error) : base(error)
    {
    }
}