using YoutubeDownloader.Models;

namespace YoutubeDownloader.Extensions;

public static class DownloadContextExtensions
{
    public static DownloadResult.Success Success(
        this DownloadContext self,
        string fileName,
        double fileSizeInMB) =>
        new(self.VideoId,
            fileName,
            self.Configuration,
            DateTime.Now,
            fileSizeInMB);

    public static DownloadResult.Failure Failure(
        this DownloadContext self,
        string errorMessage) =>
        new(self.VideoId,
            self.Configuration,
            DateTime.Now,
            errorMessage);
}