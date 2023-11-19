using Spectre.Console;
using System.Collections.ObjectModel;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Models;
using YoutubeExplode.Videos;
using AnsiConsoleExtensions = YoutubeDownloader.Extensions.AnsiConsoleExtensions;

namespace YoutubeDownloader.Services;

public sealed class YoutubeService(YoutubeDownloaderService youtubeDownloaderService)
{
    private readonly YoutubeDownloaderService _youtubeDownloaderService = youtubeDownloaderService;

    private async Task<IReadOnlyCollection<DownloadResult>> DownloadAsync(
        IReadOnlyCollection<DownloadContext> downloadContexts,
        ProgressContext progressContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var semaphore = new SemaphoreSlim(30);

            var downloadTasks = downloadContexts.Select<DownloadContext, Task<DownloadResult>>(async downloadContext =>
            {
                await semaphore.WaitAsync(cancellationToken);

                var downloadTask = progressContext.AddTask($"[green]{downloadContext.VideoId}[/]");

                var progress = new Progress<double>(value => downloadTask.Increment(value));

                try
                {
                    return await _youtubeDownloaderService.DownloadAsync(
                                                              downloadContext,
                                                              progress,
                                                              cancellationToken)
                                                          .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    downloadTask.StopTask();

                    return downloadContext.Failure(ex.Message.Replace(',', '.'));
                }
                finally
                {
                    semaphore.Release();
                }
            });

            return await Task.WhenAll(downloadTasks)
                             .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsoleExtensions.MarkupLine($"An error occurred while downloading data: ", ex.Message, AnsiColor.Red);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFromVideoLinkAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var videoId = AnsiConsoleExtensions.PromptVideoId();

        var downloadContext = getDownloadContext(videoId);

        return await AnsiConsoleExtensions.ShowProgressAsync(async ctx => await DownloadAsync([downloadContext], ctx).ConfigureAwait(false))
                                          .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFromPlaylistLinkAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var playlistId = AnsiConsoleExtensions.PromptPlaylistId();

        var videoIds = await _youtubeDownloaderService.GetVideoIdsFromPlaylistAsync(playlistId, cancellationToken);

        var downloadContext = videoIds.Select(getDownloadContext)
                                      .AsReadOnlyList();

        return await AnsiConsoleExtensions.ShowProgressAsync(ctx => DownloadAsync(downloadContext, ctx))
                                          .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFromYouTubeExportedFileAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var exportedFilePath = AnsiConsoleExtensions.PromptExportedFilePath();

        var lines = await File.ReadAllLinesAsync(exportedFilePath, cancellationToken)
                              .ConfigureAwait(false);

        var downloadContexts = lines.Skip(1)
                                   .Where(l => !string.IsNullOrWhiteSpace(l))
                                   .Select(l => getDownloadContext(GetVideoId(l)))
                                   .AsReadOnlyList();

        return await AnsiConsoleExtensions.ShowProgressAsync(ctx => DownloadAsync(downloadContexts, ctx))
                                          .ConfigureAwait(false);

        static VideoId GetVideoId(string line) =>
            line.Split(',')
                .First()
                .Trim();
    }

    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFromFromFailedDownloadsAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        Func<CancellationToken, Task<IReadOnlyCollection<DownloadResult.Failure>>> getFailedDownloads,
        CancellationToken cancellationToken)
    {
        var emptyDownloadResults = ReadOnlyCollection<DownloadResult>.Empty;
        IReadOnlyCollection<DownloadResult.Failure> failedDownloads = ReadOnlyCollection<DownloadResult.Failure>.Empty;

        try
        {
            failedDownloads = await getFailedDownloads(cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            AnsiConsoleExtensions.MarkupLine("failed downloads ", "folder not found.", AnsiColor.Red);
            return emptyDownloadResults;
        }

        if (failedDownloads.Count == 0)
        {
            AnsiConsoleExtensions.MarkupLine("failed downloads ", "not found.", AnsiColor.Red);
            return emptyDownloadResults;
        }

        var failedDownloadResendSetting = AnsiConsoleExtensions.SelectFailedDownloadResendSettings(
            [
                FailedDownloadResendSetting.KeepOriginal,
                FailedDownloadResendSetting.OverrideWithNew
            ]);

        var downloadContexts = failedDownloads.Select(f => f.ToDownloadContext());

        downloadContexts = failedDownloadResendSetting is FailedDownloadResendSetting.OverrideWithNew
                               ? downloadContexts.Select(d => getDownloadContext(d.VideoId))
                               : downloadContexts;

        return await AnsiConsoleExtensions.ShowProgressAsync(ctx => DownloadAsync(downloadContexts.AsReadOnlyList(), ctx))
                                          .ConfigureAwait(false);
    }
}
