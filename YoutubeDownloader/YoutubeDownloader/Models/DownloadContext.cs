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

public abstract record VideoConfiguration
{
    public sealed record MP3(AudioQuality AudioQuality = AudioQuality.HighBitrate) : VideoConfiguration;
    public sealed record MP4(VideoQuality VideoQuality = VideoQuality.HD) : VideoConfiguration;
}

public sealed record DownloadContext(VideoId VideoId, VideoConfiguration Configuration);