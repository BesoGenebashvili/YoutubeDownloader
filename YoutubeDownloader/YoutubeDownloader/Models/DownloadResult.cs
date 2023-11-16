using YoutubeExplode.Videos;

namespace YoutubeDownloader.Models;

public abstract record DownloadResult(
    VideoId VideoId,
    VideoConfiguration Configuration,
    DateTime Timestamp)
{
    public sealed record Success(
        VideoId VideoId,
        string FileName,
        VideoConfiguration Configuration,
        DateTime Timestamp,
        double FileSizeInMB) : DownloadResult(VideoId, Configuration, Timestamp);

    public sealed record Failure(
        VideoId VideoId,
        VideoConfiguration Configuration,
        DateTime Timestamp,
        string ErrorMessage) : DownloadResult(VideoId, Configuration, Timestamp);
}