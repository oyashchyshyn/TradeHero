using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Application.Data.Dtos.Instance;
using TradeHero.Core.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;

namespace TradeHero.Application.Data.Validations;

internal class SpotClusterVolumeOptionsDtoValidation : AbstractValidator<SpotClusterVolumeOptionsDto>
{
    private readonly ILogger<SpotClusterVolumeOptionsDtoValidation> _logger;
    private readonly ITelegramService _telegramService;
    
    private readonly Dictionary<string, string> _propertyNames = typeof(SpotClusterVolumeOptionsDto).GetPropertyNameAndJsonPropertyName();

    public SpotClusterVolumeOptionsDtoValidation(
        ILogger<SpotClusterVolumeOptionsDtoValidation> logger,
        ITelegramService telegramService
        )
    {
        _logger = logger;
        _telegramService = telegramService;

        RuleSet(ValidationRuleSet.Default.ToString(), () =>
        {
            RuleFor(x => x.ItemsInTask)
                .CustomAsync(ValidateItemsInTaskAsync);

            RuleFor(x => x.VolumeAverage)
                .CustomAsync(ValidateVolumeAverageAsync);
        
            RuleFor(x => x.OrderBookDepthPercent)
                .CustomAsync(ValidateOrderBookDepthPercentAsync);
            
            RuleFor(x => x.TelegramChannelId)
                .CustomAsync(ValidateTelegramChannelIdAsync)
                .When(x => x.TelegramChannelId.HasValue);

            RuleFor(x => x.TelegramChannelName)
                .CustomAsync(ValidateTelegramChannelNameAsync)
                .When(x => x.TelegramChannelName != null);
            
            RuleFor(x => x.TelegramIsNeedToSendMessages)
                .CustomAsync(ValidateTelegramIsNeedToSendMessagesAsync)
                .When(x => x.TelegramChannelId.HasValue && x.TelegramIsNeedToSendMessages != null && x.TelegramIsNeedToSendMessages.Value);
        });
    }

    #region Private methods

    private Task ValidateItemsInTaskAsync(int itemsInTask, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (itemsInTask)
            {
                case < 1:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.ItemsInTask)], 
                        "Cannot be lower then 1."));
                    return Task.CompletedTask;
                case > 200:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.ItemsInTask)], 
                        "Cannot be higher then 200."));
                    return Task.CompletedTask;
            }
            
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateItemsInTaskAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.ItemsInTask)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateVolumeAverageAsync(int volumeAverage, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (volumeAverage)
            {
                case < 0:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.VolumeAverage)], 
                        "Cannot be lower then 0."));
                    return Task.CompletedTask;
                case > 10000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.VolumeAverage)], 
                        "Cannot be higher then 10000."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateVolumeAverageAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.VolumeAverage)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }
    
    private Task ValidateOrderBookDepthPercentAsync(decimal orderBookDepthPercent, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (orderBookDepthPercent)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.OrderBookDepthPercent)], 
                        "Cannot be lower then 0.01."));
                    return Task.CompletedTask;
                case > 20.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.OrderBookDepthPercent)], 
                        "Cannot be higher then 20.00."));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateOrderBookDepthPercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.OrderBookDepthPercent)]}", 
                "Validation failed."));
            
            return Task.CompletedTask;
        }
    }

    private async Task ValidateTelegramChannelIdAsync(long? telegramChannelId, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (telegramChannelId)
            {
                case null:
                    return;
                case > -1:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)], 
                        "Cannot be higher then -1."));
                    break;
            }

            var result = await _telegramService.GetBotChat(
                telegramChannelId.Value,
                cancellationToken: cancellationToken
            );
        
            if (result.ActionResult != ActionResult.Success)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)], 
                    $"Bot does not has access to channel with id '{telegramChannelId}'"));
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTelegramChannelIdAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)]}", 
                "Validation failed."));
        }
    }

    private async Task ValidateTelegramChannelNameAsync(string? telegramChannelName, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (telegramChannelName == null)
            {
                return;
            }
            
            switch (telegramChannelName.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                        "Length cannot be lower then 3 symbols."));
                    return;
                case > 125:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                        "Length cannot be higher then 125 symbols."));
                    return;
            }

            if (!propertyContext.InstanceToValidate.TelegramChannelId.HasValue)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                    $"Cannot have name because '{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)]}' does not have value."));

                return;
            }
            
            var result = await _telegramService.GetBotChat(
                propertyContext.InstanceToValidate.TelegramChannelId.Value,
                cancellationToken: cancellationToken
            );

            if (result.ActionResult == ActionResult.Success)
            {
                return;
            }
            
            propertyContext.AddFailure(new ValidationFailure(
                _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                "Cannot change name because bot does not have access to channel."));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTelegramChannelIdAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)]}", 
                "Validation failed."));
        }
    }
    
    private async Task ValidateTelegramIsNeedToSendMessagesAsync(bool? telegramIsNeedToSendMessages, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (telegramIsNeedToSendMessages == null || !telegramIsNeedToSendMessages.Value)
            {
                return;
            }

            if (!propertyContext.InstanceToValidate.TelegramChannelId.HasValue)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramIsNeedToSendMessages)], 
                    $"Bot cannot send messages because '{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)]}' does not have value."));

                return;
            }
            
            var result = await _telegramService.GetBotChat(
                propertyContext.InstanceToValidate.TelegramChannelId.Value,
                cancellationToken: cancellationToken
            );

            if (result.ActionResult == ActionResult.Success)
            {
                return;
            }
            
            propertyContext.AddFailure(new ValidationFailure(
                _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramIsNeedToSendMessages)], 
                "Bot cannot send messaged because bot does not have access to channel."));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTelegramChannelIdAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramIsNeedToSendMessages)]}", 
                "Validation failed."));
        }
    }

    #endregion
}