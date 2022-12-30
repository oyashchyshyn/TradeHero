using System.Text;
using FluentValidation.Results;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Extensions;
using TradeHero.EntryPoint.Data.Dtos.Instance;
using TradeHero.EntryPoint.Data.Dtos.Strategy;

namespace TradeHero.EntryPoint.Menu.Telegram.Helpers;

internal static class MessageGenerator
{
    public static string GenerateValidationErrorMessage(List<ValidationFailure> validationFailures, Type type)
    {
        var propertyNameWithJsonPropertyName = type.GetPropertyNameAndJsonPropertyName();
        
        var stringBuilder = new StringBuilder();
        
        stringBuilder.Append($"There was an error during data validation. Check errors:{Environment.NewLine}{Environment.NewLine}");
        
        foreach (var validationFailure in validationFailures)
        {
            stringBuilder.Append(
                $"<b>{propertyNameWithJsonPropertyName[validationFailure.PropertyName]}</b> - {validationFailure.ErrorMessage}{Environment.NewLine}"
            );
        }
        
        return stringBuilder.ToString();
    }
    
    public static string GenerateCreateStrategyTypeMessage(StrategyType strategyType)
    {
        var propertyNamesWithDescription = strategyType switch
        {
            StrategyType.PercentLimit => typeof(PercentLimitStrategyDto).GetJsonPropertyNameAndDescriptionFromType(),
            StrategyType.PercentMove => typeof(PercentMoveStrategyDto).GetJsonPropertyNameAndDescriptionFromType(),
            StrategyType.NoStrategy => throw new ArgumentOutOfRangeException(nameof(strategyType), strategyType, null),
            _ => throw new ArgumentOutOfRangeException(nameof(strategyType), strategyType, null)
        };

        var stringBuilder = new StringBuilder();
        
        foreach (var propertyNameWithDescription in propertyNamesWithDescription)
        {
            stringBuilder.Append($"<b>{propertyNameWithDescription.Key}</b> - <i>{propertyNameWithDescription.Value}</i>{Environment.NewLine}");
        }
        
        return stringBuilder.ToString();
    }

    public static string GenerateCreateInstanceTypeMessage(InstanceType instanceType)
    {
        var propertyNamesWithDescription = instanceType switch
        {
            InstanceType.SpotClusterVolume => typeof(ClusterVolumeInstanceDto).GetJsonPropertyNameAndDescriptionFromType(),
            InstanceType.NoInstance => throw new ArgumentOutOfRangeException(nameof(instanceType), instanceType, null),
            _ => throw new ArgumentOutOfRangeException(nameof(instanceType), instanceType, null)
        };

        var stringBuilder = new StringBuilder();
        
        foreach (var propertyNameWithDescription in propertyNamesWithDescription)
        {
            stringBuilder.Append($"<b>{propertyNameWithDescription.Key}</b> - <i>{propertyNameWithDescription.Value}</i>{Environment.NewLine}");
        }
        
        return stringBuilder.ToString();
    }
}