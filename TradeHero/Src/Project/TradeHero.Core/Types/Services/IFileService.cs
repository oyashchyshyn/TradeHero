namespace TradeHero.Core.Types.Services;

public interface IFileService
{
    Task DeleteFilesInFolderAsync(string pathToFolder, double olderThenMilliseconds);
}