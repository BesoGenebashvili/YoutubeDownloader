﻿using FluentValidation;
using Microsoft.Extensions.Options;
using YoutubeDownloader.Settings;
using YoutubeDownloader.Models;
using YoutubeDownloader.Extensions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Channels;
using YoutubeExplode.Converter;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using Serilog;
using VideoQuality = YoutubeDownloader.Models.VideoQuality;

namespace YoutubeDownloader.Services;

public sealed class YoutubeDownloaderService(YoutubeClient youtubeClient, IOptions<DownloaderSettings> options)
{
    private readonly YoutubeClient _youtubeClient = youtubeClient;
    private readonly DownloaderSettings _settings = options.Value;

    public ValueTask<List<PlaylistVideo>> ListPlaylistVideosAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Listing playlist videos for playlistId: {playlistId}", playlistId);

        return _youtubeClient.Playlists
                             .GetVideosAsync(
                                  playlistId,
                                  cancellationToken)
                             .ToListAsync(cancellationToken);
    }

    public ValueTask<List<PlaylistVideo>> ListChannelUploadsAsync(
        ChannelId channelId,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Listing channel uploads for channelId: {channelId}", channelId);

        return _youtubeClient.Channels
                             .GetUploadsAsync(
                                 channelId,
                                 cancellationToken)
                             .ToListAsync(cancellationToken);
    }

    public async Task<DownloadResult.Success> DownloadAsync(
        DownloadContext downloadContext,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Downloading content downloadContext: {downloadContext}", downloadContext);

        var video = await _youtubeClient.Videos
                                        .GetAsync(downloadContext.VideoId, cancellationToken)
                                        .ConfigureAwait(false);

        var streamManifest = await _youtubeClient.Videos
                                                 .Streams
                                                 .GetManifestAsync(
                                                     downloadContext.VideoId,
                                                     cancellationToken)
                                                 .ConfigureAwait(false);

        var fileName = video.ResolveFileName(_settings.FileNameTemplate);

        var downloadTask = downloadContext.Configuration switch
        {
            VideoConfiguration.MP3(AudioQuality quality) =>
                DownloadMP3Async(
                    Path.Combine(_settings.SaveFolderPath, $"{fileName}.mp3"),
                    quality,
                    streamManifest,
                    progress,
                    cancellationToken),

            VideoConfiguration.MP4(VideoQuality quality) =>
                DownloadMP4Async(
                    Path.Combine(_settings.SaveFolderPath, $"{fileName}.mp4"),
                    quality,
                    streamManifest,
                    progress,
                    cancellationToken),
            _ => throw new NotImplementedException(nameof(downloadContext))
        };

        var fileSizeInMb = await downloadTask.ConfigureAwait(false);

        return downloadContext.Success(fileName, fileSizeInMb);
    }

    private async Task<double> DownloadMP3Async(
        string filePath,
        AudioQuality quality,
        StreamManifest streamManifest,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default)
    {
        var audioOnlyStreamInfos = streamManifest.GetAudioOnlyStreams();

        var streamInfo = quality switch
        {
            AudioQuality.LowBitrate => audioOnlyStreamInfos.MinBy(a => a.Bitrate) ?? throw new InvalidOperationException("Input stream collection is empty."),
            AudioQuality.HighBitrate => audioOnlyStreamInfos.GetWithHighestBitrate(),
            _ => throw new NotImplementedException(nameof(quality))
        };

        var conversionRequestBuilder = new ConversionRequestBuilder(filePath)
                                               .SetContainer(Container.Mp3)
                                               .SetPreset(ConversionPreset.Medium);

        if (!string.IsNullOrWhiteSpace(_settings.FFmpegPath))
        {
            conversionRequestBuilder.SetFFmpegPath(_settings.FFmpegPath);
        }

        await _youtubeClient.Videos
                            .DownloadAsync(
                                [streamInfo],
                                conversionRequestBuilder.Build(),
                                progress,
                                cancellationToken)
                            .ConfigureAwait(false);

        return streamInfo.Size.MegaBytes;
    }

    private async Task<double> DownloadMP4Async(
        string filePath,
        VideoQuality quality,
        StreamManifest streamManifest,
        IProgress<double>? progress = default,
        CancellationToken cancellationToken = default)
    {
        var downloadTask = quality switch
        {
            VideoQuality.SD => DownloadMuxedStreamAsync("480p"),
            VideoQuality.HD => DownloadMuxedStreamAsync("720p"),
            VideoQuality.FullHD => DownloadSeparateStreamsAsync(),
            _ => throw new NotImplementedException(nameof(quality))
        };

        return await downloadTask.ConfigureAwait(false);

        async Task<double> DownloadMuxedStreamAsync(string qualityLabel)
        {
            var muxedStreamInfos = streamManifest.GetMuxedStreams()
                                                 .Where(s => s.Container == Container.Mp4)
                                                 .OrderByDescending(s => s.VideoQuality);

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
