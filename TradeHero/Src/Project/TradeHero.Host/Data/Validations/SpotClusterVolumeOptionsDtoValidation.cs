using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Extensions;
using TradeHero.Host.Data.Dtos.Instance;

namespace TradeHero.Host.Data.Validations;

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
                .MustAsync(ValidateItemsInTaskAsync);

            RuleFor(x => x.VolumeAverage)
                .MustAsync(ValidateVolumeAverageAsync);
        
            RuleFor(x => x.OrderBookDepthPercent)
                .MustAsync(ValidateOrderBookDepthPercentAsync);
            
            RuleFor(x => x.TelegramChannelId)
                .MustAsync(ValidateTelegramChannelIdAsync)
                .When(x => x.TelegramChannelId.HasValue);

            RuleFor(x => x.TelegramChannelName)
                .MustAsync(ValidateTelegramChannelNameAsync)
                .When(x => x.TelegramChannelName != null);
            
            RuleFor(x => x.TelegramIsNeedToSendMessages)
                .MustAsync(ValidateTelegramIsNeedToSendMessagesAsync)
                .When(x => x.TelegramChannelId.HasValue && x.TelegramIsNeedToSendMessages != null && x.TelegramIsNeedToSendMessages.Value);
        });
    }

    #region Private methods

    private Task<bool> ValidateItemsInTaskAsync(SpotClusterVolumeOptionsDto spotClusterVolumeOptionsDto, 
        int itemsInTask, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (itemsInTask)
            {
                case < 1:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.ItemsInTask)], 
                        "Cannot be lower then 1."));
                    return Task.FromResult(false);
                case > 200:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.ItemsInTask)], 
                        "Cannot be higher then 200."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateItemsInTaskAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.ItemsInTask)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateVolumeAverageAsync(SpotClusterVolumeOptionsDto spotClusterVolumeOptionsDto, 
        int volumeAverage, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (volumeAverage)
            {
                case < 0:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.VolumeAverage)], 
                        "Cannot be lower then 0."));
                    return Task.FromResult(false);
                case > 10000:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.VolumeAverage)], 
                        "Cannot be higher then 10000."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateVolumeAverageAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.VolumeAverage)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }
    
    private Task<bool> ValidateOrderBookDepthPercentAsync(SpotClusterVolumeOptionsDto spotClusterVolumeOptionsDto, 
        decimal orderBookDepthPercent, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (orderBookDepthPercent)
            {
                case < 0.01m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.OrderBookDepthPercent)], 
                        "Cannot be lower then 0.01."));
                    return Task.FromResult(false);
                case > 20.00m:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.OrderBookDepthPercent)], 
                        "Cannot be higher then 20.00."));
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateOrderBookDepthPercentAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.OrderBookDepthPercent)]}", 
                "Validation failed."));
            
            return Task.FromResult(false);
        }
    }

    private async Task<bool> ValidateTelegramChannelIdAsync(SpotClusterVolumeOptionsDto spotClusterVolumeOptionsDto, 
        long? telegramChannelId, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            switch (telegramChannelId)
            {
                case null:
                    return false;
                case > -1:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)], 
                        "Cannot be higher then -1."));
                    return false;
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

                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTelegramChannelIdAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)]}", 
                "Validation failed."));
            
            return false;
        }
    }

    private async Task<bool> ValidateTelegramChannelNameAsync(SpotClusterVolumeOptionsDto spotClusterVolumeOptionsDto, 
        string? telegramChannelName, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (telegramChannelName == null)
            {
                return false;
            }
            
            switch (telegramChannelName.Length)
            {
                case < 3:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                        "Length cannot be lower then 3 symbols."));
                    return false;
                case > 125:
                    propertyContext.AddFailure(new ValidationFailure(
                        _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                        "Length cannot be higher then 125 symbols."));
                    return false;
            }

            if (!spotClusterVolumeOptionsDto.TelegramChannelId.HasValue)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                    $"Cannot have name because '{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)]}' does not have value."));

                return false;
            }
            
            var result = await _telegramService.GetBotChat(
                spotClusterVolumeOptionsDto.TelegramChannelId.Value,
                cancellationToken: cancellationToken
            );
        
            if (result.ActionResult != ActionResult.Success)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                    "Cannot change name because bot does not have access to channel."));

                return false;
            }

            if (result.Data.Permissions is not { CanChangeInfo: { } } || !result.Data.Permissions.CanChangeInfo.Value)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)], 
                    "Bot does not have permissions to change channel name. Please add permissions to bot."));

                return false;
            }
            
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTelegramChannelIdAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelName)]}", 
                "Validation failed."));
            
            return false;
        }
    }
    
    private async Task<bool> ValidateTelegramIsNeedToSendMessagesAsync(SpotClusterVolumeOptionsDto spotClusterVolumeOptionsDto, 
        bool? telegramIsNeedToSendMessages, ValidationContext<SpotClusterVolumeOptionsDto> propertyContext, CancellationToken cancellationToken)
    {
        try
        {
            if (telegramIsNeedToSendMessages == null || !telegramIsNeedToSendMessages.Value)
            {
                return true;
            }

            if (!spotClusterVolumeOptionsDto.TelegramChannelId.HasValue)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramIsNeedToSendMessages)], 
                    $"Bot cannot send messages because '{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramChannelId)]}' does not have value."));

                return false;
            }
            
            var result = await _telegramService.GetBotChat(
                spotClusterVolumeOptionsDto.TelegramChannelId.Value,
                cancellationToken: cancellationToken
            );
        
            if (result.ActionResult != ActionResult.Success)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramIsNeedToSendMessages)], 
                    "Bot cannot send messaged because bot does not have access to channel."));

                return false;
            }

            if (result.Data.Permissions is not { CanSendMessages: { } } || !result.Data.Permissions.CanSendMessages.Value)
            {
                propertyContext.AddFailure(new ValidationFailure(
                    _propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramIsNeedToSendMessages)], 
                    "Bot does not have permissions to send messages. Please add permissions to bot."));

                return false;
            }
            
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ValidateTelegramChannelIdAsync));
            
            propertyContext.AddFailure(new ValidationFailure(
                $"{_propertyNames[nameof(SpotClusterVolumeOptionsDto.TelegramIsNeedToSendMessages)]}", 
                "Validation failed."));
            
            return false;
        }
    }

    #endregion
}