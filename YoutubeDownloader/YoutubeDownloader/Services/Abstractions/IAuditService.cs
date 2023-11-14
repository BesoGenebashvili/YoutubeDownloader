using YoutubeDownloader.Models;

namespace YoutubeDownloader.Services.Abstractions;

public interface IAuditService
{
    Task AuditDownloadsAsync(
        IReadOnlyCollection<DownloadResult> downloads,
        CancellationToken cancellationToken = default);
}