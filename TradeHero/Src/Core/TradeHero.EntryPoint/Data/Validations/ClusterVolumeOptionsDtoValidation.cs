using FluentValidation;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Services;
using TradeHero.EntryPoint.Data.Dtos.Instance;

namespace TradeHero.EntryPoint.Data.Validations;

internal class ClusterVolumeOptionsDtoValidation : AbstractValidator<ClusterVolumeInstanceDto>
{
    private readonly ITelegramService _telegramService;
    
    public ClusterVolumeOptionsDtoValidation(
        ITelegramService telegramService
        )
    {
        _telegramService = telegramService;

        RuleSet(ValidationRuleSet.Default.ToString(), () =>
        {
            RuleFor(x => x.TelegramChannelId)
                .MustAsync(IsBotHasAccessToChannel).When(x => x.TelegramChannelId.HasValue)
                .WithMessage(x => $"Bot does not has access to channel with id '{x.TelegramChannelId}'")
                .LessThanOrEqualTo(-1).When(x => x.TelegramChannelId.HasValue);
        
            RuleFor(x => x.ItemsInTask)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(200);
        
            RuleFor(x => x.TelegramChannelName)
                .NotEmpty().When(x => x.TelegramChannelName != null)
                .MinimumLength(3).When(x => x.TelegramChannelName != null)
                .MaximumLength(125).When(x => x.TelegramChannelName != null);

            RuleFor(x => x.ItemsInTask)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(10000);
        
            RuleFor(x => x.VolumeAverage)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(10000);
        
            RuleFor(x => x.OrderBookDepthPercent)
                .GreaterThanOrEqualTo(0.01m)
                .LessThanOrEqualTo(20.00m);
        });
    }

    #region Private methods

    private async Task<bool> IsBotHasAccessToChannel(ClusterVolumeInstanceDto clusterVolumeInstanceDto, long? telegramChannelId, CancellationToken cancellationToken)
    {
        if (!telegramChannelId.HasValue)
        {
            return false;
        }
        
        var result = await _telegramService.GetBotChat(
            telegramChannelId.Value,
            cancellationToken: cancellationToken
        );

        return result.ActionResult == ActionResult.Success;
    }

    #endregion
}