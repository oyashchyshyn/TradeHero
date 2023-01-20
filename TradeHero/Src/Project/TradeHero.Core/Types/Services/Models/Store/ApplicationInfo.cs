namespace TradeHero.Core.Types.Services.Models.Store;

public class ApplicationInfo
{
    public UpdateInfo Update { get; } = new();
    public ErrorInfo Errors { get; } = new();
}