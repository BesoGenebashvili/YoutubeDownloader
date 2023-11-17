using YoutubeDownloader.Models;

namespace YoutubeDownloader.Services;

public interface IAuditService
{
    Task AuditDownloadsAsync(
        IReadOnlyCollection<DownloadResult> downloads,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DownloadResult.Success>> ListSuccessfulDownloadsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DownloadResult.Failure>> ListFailedDownloadsAsync(
        CancellationToken cancellationToken = default);
}