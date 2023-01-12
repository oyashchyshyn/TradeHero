namespace TradeHero.Core.Settings.AppSettings;

public class ApplicationSettings
{
    public NameSettings WindowsNames { get; set; } = new();
    public NameSettings LinuxNames { get; set; } = new();
}