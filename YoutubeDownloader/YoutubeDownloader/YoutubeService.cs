using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace YoutubeDownloader;

public sealed record DownloaderSettings(
    string SaveFolderPath, 
    string? FFmpegPath);

public sealed class YoutubeService(YoutubeClient youtubeClient, IOptions<DownloaderSettings> options)
{
    private readonly YoutubeClient _youtubeClient = youtubeClient;
    private readonly DownloaderSettings _settings = options.Value;

    private static string ResolveFilename(string title, VideoId videoId)
    {
        var regexPattern = $"[{Regex.Escape(new(Path.GetInvalidFileNameChars()))}]";

        var newFilename = Regex.Replace(title, regexPattern, string.Empty);

        return string.IsNullOrWhiteSpace(newFilename)
                     ? videoId
                     : newFilename;
    }

    public async Task<(string fileName, double fileSizeInMB)> DownloadMP3Async(
        VideoId videoId,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default)
    {
        var video = await _youtubeClient.Videos
                                        .GetAsync(videoId, cancellationToken)
                                        .ConfigureAwait(false);

        var streamManifest = await _youtubeClient.Videos
                                                 .Streams
                                                 .GetManifestAsync(videoId, cancellationToken)
                                                 .ConfigureAwait(false);

        var streamInfo = streamManifest.GetAudioOnlyStreams()
                                       .GetWithHighestBitrate();

        var fileName = ResolveFilename(video.Title, videoId);
        var filePath = Path.Combine(_settings.SaveFolderPath, $"{fileName}.mp3");

        await _youtubeClient.Videos
                            .Streams
                            .DownloadAsync(
                                 streamInfo,
                                 filePath,
                                 progress,
                                 cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

        return (fileName, streamInfo.Size.MegaBytes);
    }

    public async Task<string> DownloadMP4Async(
        VideoId videoId,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default)
    {
        var video = await _youtubeClient.Videos
                                        .GetAsync(videoId, cancellationToken)
                                        .ConfigureAwait(false);

        var streamManifest = await _youtubeClient.Videos
                                                 .Streams
                                                 .GetManifestAsync(
                                                     videoId,
                                                     cancellationToken)
                                                 .ConfigureAwait(false);

        var audioStreamInfo = streamManifest.GetAudioStreams()
                                            .Where(s => s.Container == Container.Mp4)
                                            .GetWithHighestBitrate();

        var videoStreamInfo = streamManifest.GetVideoStreams()
                                            .Where(s => s.Container == Container.Mp4)
                                            .GetWithHighestVideoQuality();

        var fileName = ResolveFilename(video.Title, videoId);
        var filePath = Path.Combine(_settings.SaveFolderPath, $"{fileName}.mp4");

        var streamInfos = new IStreamInfo[]
        {
            audioStreamInfo,
            videoStreamInfo
        };

        await _youtubeClient.Videos
                            .DownloadAsync(
                                 streamInfos,
                                 new ConversionRequestBuilder(filePath)
                                         .SetFFmpegPath(_settings.SaveFolderPath)
                                         .Build(),
                                 progress,
                                 cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

        return fileName;
    }
}
