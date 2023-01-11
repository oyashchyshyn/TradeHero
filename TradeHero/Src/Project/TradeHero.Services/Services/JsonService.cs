using System.Dynamic;
using CryptoExchange.Net.Converters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Core.Models;
using TradeHero.Services.ContractResolvers;

namespace TradeHero.Services.Services;

internal class JsonService : IJsonService
{
    private readonly ILogger<JsonService> _logger;

    public JsonService(ILogger<JsonService> logger)
    {
        _logger = logger;
    }
    
    public GenericBaseResult<string> SerializeObject(object? value, Formatting formatting = Formatting.None, 
        JsonSerializationSettings serializationSettings = JsonSerializationSettings.None)
    {
        try
        {
            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = formatting
            };

            serializerSettings.Converters.Add(new EnumConverter());
            
            if (serializationSettings == JsonSerializationSettings.IgnoreJsonPropertyName)
            {
                serializerSettings.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }
        
            var result = JsonConvert.SerializeObject(value, serializerSettings);

            return new GenericBaseResult<string>(result);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(SerializeObject));
            
            return new GenericBaseResult<string>(ActionResult.SystemError);
        }
    }

    public GenericBaseResult<T> Deserialize<T>(string json, JsonSerializationSettings serializationSettings = JsonSerializationSettings.None)
    {
        try
        {
            var serializerSettings = new JsonSerializerSettings();
            
            serializerSettings.Converters.Add(new EnumConverter());

            if (serializationSettings == JsonSerializationSettings.IgnoreJsonPropertyName)
            {
                serializerSettings.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }
            
            var result = JsonConvert.DeserializeObject<T>(json, serializerSettings);

            if (result != null)
            {
                return new GenericBaseResult<T>(result);
            }

            _logger.LogWarning("DeserializeObject result is null. In {Method}", 
                nameof(Deserialize));

            return new GenericBaseResult<T>(ActionResult.Error);

        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(SerializeObject));
            
            return new GenericBaseResult<T>(ActionResult.SystemError);
        }
    }
    
    public GenericBaseResult<object> Deserialize(string json, Type type, 
        JsonSerializationSettings serializationSettings = JsonSerializationSettings.None)
    {
        try
        {
            var serializerSettings = new JsonSerializerSettings();
            
            serializerSettings.Converters.Add(new EnumConverter());

            if (serializationSettings == JsonSerializationSettings.IgnoreJsonPropertyName)
            {
                serializerSettings.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }
            
            var result = JsonConvert.DeserializeObject(json, type, serializerSettings);

            if (result != null)
            {
                return new GenericBaseResult<object>(result);
            }
            
            _logger.LogWarning("DeserializeObject result is null. In {Method}", 
                nameof(Deserialize));

            return new GenericBaseResult<object>(ActionResult.Error);

        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(SerializeObject));
            
            return new GenericBaseResult<object>(ActionResult.SystemError);
        }
    }
    
    public GenericBaseResult<JObject> GetJObject(string json)
    {
        try
        {
            var result = JObject.Parse(json);

            return new GenericBaseResult<JObject>(result);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(SerializeObject));
            
            return new GenericBaseResult<JObject>(ActionResult.SystemError);
        }
    }

    public GenericBaseResult<JObject> GetJObject(object obj,
        JsonSerializationSettings serializationSettings = JsonSerializationSettings.None)
    {
        try
        {
            var serializerSettings = new JsonSerializer();
            
            serializerSettings.Converters.Add(new EnumConverter());

            if (serializationSettings == JsonSerializationSettings.IgnoreJsonPropertyName)
            {
                serializerSettings.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }
            
            var result = JObject.FromObject(obj, serializerSettings);

            return new GenericBaseResult<JObject>(result);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(SerializeObject));
            
            return new GenericBaseResult<JObject>(ActionResult.SystemError);
        }
    }
    
    public GenericBaseResult<ExpandoObject> ConvertKeyValueStringDataToDictionary(string stringData, 
        JsonKeyTransformation jsonKeyTransformation = JsonKeyTransformation.Default)
    {
        try
        {
            var dictionary = new ExpandoObject();
            foreach (var assignment in stringData.Split("\n"))
            {
                var sections = assignment.Split(new[] { ":", "=" }, StringSplitOptions.RemoveEmptyEntries);

                var key = sections[0].Trim();
                var value = sections[1].Trim();

                if (jsonKeyTransformation == JsonKeyTransformation.ToCapitaliseCase)
                {
                    key = key.CapitalizeFirstLetter();
                }
                
                if (value.Contains('\r'))
                {
                    value = value.Replace("\r", string.Empty);
                }
                
                if (value.Contains('[') && value.Contains(']'))
                {
                    value = value.Replace("[", string.Empty)
                        .Replace("]", string.Empty);

                    value = value.Replace(@"\", string.Empty);
                    
                    var collection = value.Contains(',') 
                            ? value.Split(',').Select(x => x.Trim()) 
                            : new[] { value.Trim() };
                    
                    dictionary.TryAdd(key, collection);
                    
                    continue;
                }

                if (value.StartsWith("True") || value.StartsWith("False"))
                {
                    dictionary.TryAdd(key, bool.Parse(value));
                    
                    continue;
                }
                
                dictionary.TryAdd(key, value);
            }

            return new GenericBaseResult<ExpandoObject>(dictionary); 
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(ConvertKeyValueStringDataToDictionary));
            
            return new GenericBaseResult<ExpandoObject>(ActionResult.SystemError);
        }
    }
}