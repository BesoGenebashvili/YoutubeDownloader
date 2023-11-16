using YoutubeDownloader.Models;

namespace YoutubeDownloader.Extensions;

public static class DownloadContextExtensions
{
    public static DownloadResult.Success Success(
        this DownloadContext downloadContext,
        string fileName,
        double fileSizeInMB) =>
        new(downloadContext.VideoId,
            fileName,
            downloadContext.Configuration,
            DateTime.Now,
            fileSizeInMB);

    public static DownloadResult.Failure Failure(
        this DownloadContext downloadContext,
        string errorMessage) =>
        new(downloadContext.VideoId,
            downloadContext.Configuration,
            DateTime.Now,
            errorMessage);
}