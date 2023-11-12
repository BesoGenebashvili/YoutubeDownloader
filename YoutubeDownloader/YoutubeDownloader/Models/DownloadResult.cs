using YoutubeExplode.Videos;

namespace YoutubeDownloader.Models;

// TODO: add quality
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

//TODO: this should not be here
public static class DownloadResultExtensions
{
    private static FileFormat GetFileFormat(this DownloadContext downloadContext) => downloadContext switch
    {
        DownloadContext.MP3 => FileFormat.MP3,
        DownloadContext.MP4 => FileFormat.MP4,
        _ => throw new NotImplementedException(nameof(downloadContext))
    };

    public static DownloadResult.Success Success(
        this DownloadContext downloadContext,
        string fileName,
        double fileSizeInMB) =>
        new(downloadContext.VideoId,
            fileName,
            downloadContext.GetFileFormat(),
            DateTime.Now,
            fileSizeInMB);

    public static DownloadResult.Failure Failure(
        this DownloadContext downloadContext,
        string errorMessage) =>
        new(downloadContext.VideoId,
            downloadContext.GetFileFormat(),
            DateTime.Now,
            errorMessage);
}