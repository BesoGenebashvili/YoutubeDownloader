namespace YoutubeDownloader;

public enum FileFormat : byte
{
    MP3,
    MP4
}

public enum AudioQuality : byte
{
    LowBitrate,
    HighBitrate
}

public enum VideoQuality : byte
{
    /// <summary>
    /// 480p
    /// </summary>
    SD,

    /// <summary>
    /// 720p
    /// </summary>
    HD,

    /// <summary>
    /// 1080p
    /// </summary>
    FullHD
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