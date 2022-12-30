using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;

namespace TradeHero.Core.Services;

internal class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly IDateTimeService _dateTimeService;

    public FileService(
        ILogger<FileService> logger, 
        IDateTimeService dateTimeService
        )
    {
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public Task DeleteFilesInFolderAsync(string pathToFolder, double olderThenMilliseconds)
    {
        try
        {
            if (!Directory.Exists(pathToFolder))
            {
                _logger.LogWarning("Folder does not exist in path: {Path}. In {Method}", 
                    pathToFolder, nameof(DeleteFilesInFolderAsync));
                
                return Task.CompletedTask;
            }

            foreach (var filePath in Directory.GetFiles(pathToFolder))
            {
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.LastWriteTimeUtc.AddMilliseconds(olderThenMilliseconds) > _dateTimeService.GetUtcDateTime())
                {
                    continue;
                }
                
                fileInfo.Delete();
                    
                _logger.LogInformation("File with path: '{FilePath}' is deleted. In {Method}", filePath, 
                    nameof(DeleteFilesInFolderAsync));
            }
            
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(DeleteFilesInFolderAsync));
            
            return Task.CompletedTask;
        }
    }
}