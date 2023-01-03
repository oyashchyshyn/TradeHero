namespace TradeHero.Contracts.Extensions;

public static class HttpClientExtensions
{
    public static async Task DownloadAsync(this HttpClient client, HttpRequestMessage httpRequestMessage, 
        Stream destination, IProgress<decimal>? progress = null, CancellationToken cancellationToken = default)
    {
        // Get the http headers first to examine the content length
        var response = await client.SendAsync(httpRequestMessage, 
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        var contentLength = response.Content.Headers.ContentLength;

        await using var download = await response.Content.ReadAsStreamAsync(cancellationToken);
        
        // Ignore progress reporting when no progress reporter was 
        // passed or when the content length is unknown
        if (progress == null || !contentLength.HasValue) 
        {
            await download.CopyToAsync(destination, cancellationToken);
            return;
        }

        // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
        var relativeProgress = new Progress<long>(totalBytes => progress.Report((decimal)totalBytes / contentLength.Value));
        
        // Use extension method to report progress while downloading
        await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
        
        progress.Report(1);
    }
}