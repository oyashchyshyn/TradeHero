namespace TradeHero.Core.Models.Store;

public class ApplicationInfo
{
    public UpdateInfo Update { get; } = new();
    public ErrorInfo Errors { get; } = new();
}