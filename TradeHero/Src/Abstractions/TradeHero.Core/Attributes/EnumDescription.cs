using System.ComponentModel;

namespace TradeHero.Core.Attributes;

public class EnumDescription : DescriptionAttribute
{
    public EnumDescription(string message, Type type)
    {
        DescriptionValue = type.IsEnum 
            ? string.Format(message, string.Join(", ", Enum.GetNames(type))) 
            : string.Empty;
    }
    
    public override string Description => DescriptionValue;
}