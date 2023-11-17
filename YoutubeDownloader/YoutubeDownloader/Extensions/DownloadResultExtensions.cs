using YoutubeDownloader.Models;

namespace YoutubeDownloader.Extensions;

public static class DownloadResultExtensions
{
    public static DownloadContext ToDownloadContext(
        this DownloadResult self) =>
        new(self.VideoId, self.Configuration);
}
