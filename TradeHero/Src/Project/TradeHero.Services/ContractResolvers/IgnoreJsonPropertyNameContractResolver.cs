using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TradeHero.Services.ContractResolvers;

internal class IgnoreJsonPropertyNameContractResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var jsonPropertyList = base.CreateProperties(type, memberSerialization);

        foreach (var jsonProperty in jsonPropertyList)
        {
            jsonProperty.PropertyName = jsonProperty.UnderlyingName;
        }

        return jsonPropertyList;
    }
}