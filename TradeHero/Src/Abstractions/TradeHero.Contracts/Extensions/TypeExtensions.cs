using System.ComponentModel;
using Newtonsoft.Json;

namespace TradeHero.Contracts.Extensions;

public static class TypeExtensions
{
    public static Dictionary<string, string> GetJsonPropertyNameAndDescriptionFromType(this Type type)
    {
        var dictionary = new Dictionary<string, string>();
        
        foreach (var memberInfo in type.GetMembers())
        {
            var jsonPropertyAttributes = memberInfo.GetCustomAttributes(typeof(JsonPropertyAttribute), inherit: false);
            var descriptionAttributes = memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), inherit: true);
            
            if (jsonPropertyAttributes.Any() && jsonPropertyAttributes[0] is JsonPropertyAttribute jsonPropertyAttribute
                                             && descriptionAttributes.Any() && descriptionAttributes[0] is DescriptionAttribute descriptionAttribute)
            {
                dictionary.Add(jsonPropertyAttribute.PropertyName ?? string.Empty, descriptionAttribute.Description);
            }
        }

        return dictionary;
    }

    public static Dictionary<string, string> GetPropertyNameAndJsonPropertyName(this Type type)
    {
        var dictionary = new Dictionary<string, string>();
        
        foreach (var memberInfo in type.GetMembers())
        {
            var jsonPropertyAttributes = memberInfo.GetCustomAttributes(typeof(JsonPropertyAttribute), inherit: false);

            if (jsonPropertyAttributes.Any() && jsonPropertyAttributes[0] is JsonPropertyAttribute jsonPropertyAttribute)
            {
                dictionary.Add(memberInfo.Name, jsonPropertyAttribute.PropertyName ?? string.Empty);
            }
        }

        return dictionary;
    }
}