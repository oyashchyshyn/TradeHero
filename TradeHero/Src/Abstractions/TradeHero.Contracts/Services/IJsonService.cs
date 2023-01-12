using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradeHero.Core.Enums;
using TradeHero.Core.Models;

namespace TradeHero.Contracts.Services;

public interface IJsonService
{
    GenericBaseResult<string> SerializeObject(object? value, Formatting formatting = Formatting.None, JsonSerializationSettings serializationSettings = JsonSerializationSettings.None);
    GenericBaseResult<T> Deserialize<T>(string json, JsonSerializationSettings serializationSettings = JsonSerializationSettings.None);
    GenericBaseResult<object> Deserialize(string json, Type type, JsonSerializationSettings serializationSettings = JsonSerializationSettings.None);
    GenericBaseResult<JObject> GetJObject(string json);
    GenericBaseResult<JObject> GetJObject(object obj, JsonSerializationSettings serializationSettings = JsonSerializationSettings.None);
    GenericBaseResult<ExpandoObject> ConvertKeyValueStringDataToDictionary(string stringData, JsonKeyTransformation jsonKeyTransformation = JsonKeyTransformation.Default);
}