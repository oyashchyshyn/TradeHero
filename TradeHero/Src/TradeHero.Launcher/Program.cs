namespace TradeHero.Launcher;

internal static class Program
{
    private static Task Main(string[] args)
    {
        try
        {
            Environment.Exit(0);
        }
        catch (Exception)
        {
            Environment.Exit(-1);
        }

        return Task.CompletedTask;
    }
}