using YoutubeExplode.Videos;

namespace YoutubeDownloader.Models;

public enum DownloadOption : byte
{
    FromVideoLink,
    FromPlaylistLink,
    FromYouTubeExportedFile,
    FromFailedDownloads
}

public enum FailedDownloadResendSetting : byte
{
    KeepOriginal,
    OverrideWithNew
}

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

public abstract record DownloadContext(VideoId VideoId)
{
    public record MP3(VideoId VideoId, AudioQuality AudioQuality = AudioQuality.HighBitrate) : DownloadContext(VideoId);
    public record MP4(VideoId VideoId, VideoQuality VideoQuality = VideoQuality.HD) : DownloadContext(VideoId);
}
