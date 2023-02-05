namespace TradeHero.Core.Contracts.Services;

public interface IFileService
{
    Task DeleteFilesInFolderAsync(string pathToFolder, double olderThenMilliseconds);
}