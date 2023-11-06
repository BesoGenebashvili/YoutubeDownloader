namespace YoutubeDownloader;

public enum FileFormat : byte
{
    MP3,
    MP4
}

public abstract record DownloadResult(string VideoId, FileFormat FileFormat, DateTime Timestamp)
{
    public record Success(
        string VideoId,
        string FileName,
        FileFormat FileFormat,
        DateTime Timestamp,
        double FileSizeInMB) : DownloadResult(VideoId, FileFormat, Timestamp);

    public record Failure(
        string VideoId,
        FileFormat FileFormat,
        DateTime Timestamp,
        string ErrorMessage) : DownloadResult(VideoId, FileFormat, Timestamp);
}

public static class DownloadResultExtensions
{
    private const string CSVTimestampFormat = "yyyy-MM-dd hh:mm:ss";
}