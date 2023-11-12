using YoutubeExplode.Videos;

namespace YoutubeDownloader.Models;

public abstract record DownloadResult(VideoId VideoId, FileFormat FileFormat, DateTime Timestamp)
{
    public record Success(
        VideoId VideoId,
        string FileName,
        FileFormat FileFormat,
        DateTime Timestamp,
        double FileSizeInMB) : DownloadResult(VideoId, FileFormat, Timestamp);

    public record Failure(
        VideoId VideoId,
        FileFormat FileFormat,
        DateTime Timestamp,
        string ErrorMessage) : DownloadResult(VideoId, FileFormat, Timestamp);
}
