using YoutubeDownloader.Models;

namespace YoutubeDownloader;

public interface IAuditService
{
    Task AuditDownloadsAsync(
        IReadOnlyCollection<DownloadResult> downloads,
        CancellationToken cancellationToken = default);
}