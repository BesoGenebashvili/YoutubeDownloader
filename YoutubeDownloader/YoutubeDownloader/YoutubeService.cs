using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace YoutubeDownloader;

public sealed class DownloaderSettings
{
    public const string SectionName = "DownloaderSettings";

    public required string SaveFolderPath { get; init; }

    public string? FFmpegPath { get; init; }
}

public sealed class DownloaderSettingsValidator : AbstractValidator<DownloaderSettings>
{
    public DownloaderSettingsValidator()
    {
        // TODO: Custom logic for files
        RuleFor(s => s.SaveFolderPath)
            //.NotNull()
            .NotEmpty();
    }
}

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
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

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
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));

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
