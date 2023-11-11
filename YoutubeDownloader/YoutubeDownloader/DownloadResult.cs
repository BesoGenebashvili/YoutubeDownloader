namespace YoutubeDownloader;

public enum AudioFileFormat : byte
{
    MP3,
    MP4
}

public abstract record DownloadResult(string VideoId, AudioFileFormat FileFormat, DateTime Timestamp)
{
    public record Success(
        string VideoId,
        string FileName,
        AudioFileFormat FileFormat,
        DateTime Timestamp,
        double FileSizeInMB) : DownloadResult(VideoId, FileFormat, Timestamp);

    public record Failure(
        string VideoId,
        AudioFileFormat FileFormat,
        DateTime Timestamp,
        string ErrorMessage) : DownloadResult(VideoId, FileFormat, Timestamp);
}