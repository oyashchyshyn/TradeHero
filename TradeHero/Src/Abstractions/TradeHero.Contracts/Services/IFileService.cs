namespace TradeHero.Contracts.Services;

public interface IFileService
{
    Task DeleteFilesInFolderAsync(string pathToFolder, double olderThenMilliseconds);
}