using YoutubeDownloader.Models;

namespace YoutubeDownloader.Services.Abstractions;

public interface IAuditService
{
    Task AuditDownloadsAsync(
        IReadOnlyCollection<DownloadResult> downloads,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DownloadResult.Success>> ListSuccessfulDownloads(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DownloadResult.Failure>> ListFailedDownloadsAsync(
        CancellationToken cancellationToken = default);
}