namespace TradeHero.Core.Settings.AppSettings;

public class ApplicationSettings
{
    public string RunAppKey { get; set; } = string.Empty;
    public NameSettings WindowsNames { get; set; } = new();
    public NameSettings LinuxNames { get; set; } = new();
    public NameSettings OsxNames { get; set; } = new();
}