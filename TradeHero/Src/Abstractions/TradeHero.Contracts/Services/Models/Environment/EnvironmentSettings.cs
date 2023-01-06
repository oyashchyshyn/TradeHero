namespace TradeHero.Contracts.Services.Models.Environment;

public class EnvironmentSettings
{
    public ApplicationSettings Application { get; set; } = new();
    public ClientSettings Client { get; set; } = new();
    public InternetConnectionSettings InternetConnection { get; set; } = new();
    public LoggerSettings Logger { get; set; } = new();
    public GithubSettings Github { get; set; } = new();
    public FolderSettings Folder { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
}