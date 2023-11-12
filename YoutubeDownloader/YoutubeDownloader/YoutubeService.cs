using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using FluentValidation;
using YoutubeDownloader.Settings;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using YoutubeDownloader.Models;
using VideoQuality = YoutubeDownloader.Models.VideoQuality;

namespace YoutubeDownloader;

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

    public async Task<IEnumerable<VideoId>> GetVideoIdsFromPlaylistAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken = default)
    {
        var playlistVideos = await _youtubeClient.Playlists
                                                 .GetVideosAsync(
                                                      playlistId,
                                                      cancellationToken)
                                                 .ToListAsync(cancellationToken);

        return playlistVideos.Select(x => x.Id);
    }

    // TODO: refactor
    public Task<(string fileName, double fileSizeInMB)> DownloadAsync(
        DownloadContext downloadContext,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default) => downloadContext switch
        {
            DownloadContext.MP3 mp3 => DownloadMP3Async(mp3, progress, cancellationToken),
            DownloadContext.MP4 mp4 => DownloadMP4Async(mp4, progress, cancellationToken),
            _ => throw new NotImplementedException(nameof(downloadContext))
        };

    private async Task<(string fileName, double fileSizeInMB)> DownloadMP3Async(
        DownloadContext.MP3 downloadContext,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default)
    {
        var video = await _youtubeClient.Videos
                                        .GetAsync(downloadContext.VideoId, cancellationToken)
                                        .ConfigureAwait(false);

        var streamManifest = await _youtubeClient.Videos
                                                 .Streams
                                                 .GetManifestAsync(
                                                     downloadContext.VideoId, 
                                                     cancellationToken)
                                                 .ConfigureAwait(false);

        var audioOnlyStreamInfos = streamManifest.GetAudioOnlyStreams();

        var streamInfo = downloadContext.AudioQuality switch
        {
            AudioQuality.LowBitrate => audioOnlyStreamInfos.MinBy(a => a.Bitrate) ?? throw new InvalidOperationException("Input stream collection is empty."),
            AudioQuality.HighBitrate => audioOnlyStreamInfos.GetWithHighestBitrate(),
            _ => throw new NotImplementedException(nameof(downloadContext.AudioQuality))
        };

        var fileName = ResolveFilename(video.Title, downloadContext.VideoId);
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

    private async Task<(string fileName, double fileSizeInMB)> DownloadMP4Async(
        DownloadContext.MP4 downloadContext,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default)
    {
        var video = await _youtubeClient.Videos
                                        .GetAsync(downloadContext.VideoId, cancellationToken)
                                        .ConfigureAwait(false);

        var streamManifest = await _youtubeClient.Videos
                                                 .Streams
                                                 .GetManifestAsync(
                                                     downloadContext.VideoId,
                                                     cancellationToken)
                                                 .ConfigureAwait(false);

        var fileName = ResolveFilename(video.Title, downloadContext.VideoId);
        var filePath = Path.Combine(_settings.SaveFolderPath, $"{fileName}.mp4");

        var downloadTask = downloadContext.VideoQuality switch
        {
            VideoQuality.SD => DownloadMuxedStreamAsync("480p"),
            VideoQuality.HD => DownloadMuxedStreamAsync("720p"),
            VideoQuality.FullHD => DownloadSeparateStreamsAsync(),
            _ => throw new NotImplementedException(nameof(downloadContext.VideoQuality)),
        };

        var fileSizeInMB = await downloadTask.ConfigureAwait(false);

        return (fileName, fileSizeInMB);

        async Task<double> DownloadMuxedStreamAsync(string qualityLabel)
        {
            var muxedStreamInfos = streamManifest.GetMuxedStreams()
                                                 .Where(s => s.Container == Container.Mp4)
                                                 .OrderByDescending(s => s.VideoQuality);

            // TODO: Refactor
            var streamInfo = muxedStreamInfos.FirstOrDefault(s => s.VideoQuality.Label == qualityLabel)
                                             ?? muxedStreamInfos.First(
                                                    s => s.VideoQuality.Label != "720p"
                                                      && s.VideoQuality.Label != qualityLabel);

            await _youtubeClient.Videos
                                .Streams
                                .DownloadAsync(
                                     streamInfo,
                                     filePath,
                                     progress,
                                     cancellationToken)
                                .ConfigureAwait(false);

            return streamInfo.Size.MegaBytes;
        }

        async Task<double> DownloadSeparateStreamsAsync()
        {
            var audioStreamInfo = streamManifest.GetAudioStreams()
                                                .Where(s => s.Container == Container.Mp4)
                                                .GetWithHighestBitrate();

            var videoStreamInfo = streamManifest.GetVideoStreams()
                                                .Where(s => s.Container == Container.Mp4)
                                                .GetWithHighestVideoQuality();

            var streamInfos = new IStreamInfo[]
            {
                audioStreamInfo,
                videoStreamInfo
            };

            var conversionRequestBuilder = new ConversionRequestBuilder(filePath);

            if (!string.IsNullOrWhiteSpace(_settings.FFmpegPath))
            {
                conversionRequestBuilder.SetFFmpegPath(_settings.FFmpegPath);
            }

            await _youtubeClient.Videos
                                .DownloadAsync(
                                     streamInfos,
                                     conversionRequestBuilder.Build(),
                                     progress,
                                     cancellationToken)
                                .ConfigureAwait(false);

            return audioStreamInfo.Size.MegaBytes + videoStreamInfo.Size.MegaBytes;
        }
    }
}
