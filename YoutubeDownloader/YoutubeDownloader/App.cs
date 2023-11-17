using Spectre.Console;
using System.Collections.ObjectModel;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services.Abstractions;
using YoutubeDownloader.Services.Implementations;
using YoutubeExplode.Videos;
using AnsiConsoleExtensions = YoutubeDownloader.Extensions.AnsiConsoleExtensions;

namespace YoutubeDownloader;

public sealed class App(YoutubeService youtubeService, IAuditService auditService)
{
    private readonly YoutubeService _youtubeService = youtubeService;
    private readonly IAuditService _auditService = auditService;

    public async Task RunAsync(string[] args)
    {
        var cancellationToken = CancellationToken.None;

        var downloadOption = AnsiConsoleExtensions.SelectDownloadOption(
            [
                DownloadOption.FromVideoLink,
                DownloadOption.FromPlaylistLink,
                DownloadOption.FromYouTubeExportedFile,
                DownloadOption.FromFailedDownloads
            ]);

        var getDownloadContext = AnsiConsoleExtensions.SelectDownloadContext();

        var downloadTask = downloadOption switch
        {
            DownloadOption.FromVideoLink => DownloadFromVideoLinkAsync(getDownloadContext, cancellationToken),
            DownloadOption.FromPlaylistLink => DownloadFromPlaylistLinkAsync(getDownloadContext, cancellationToken),
            DownloadOption.FromYouTubeExportedFile => DownloadFromYouTubeExportedFileAsync(getDownloadContext, cancellationToken),
            DownloadOption.FromFailedDownloads => DownloadFromFromFailedDownloads(getDownloadContext, cancellationToken),
            _ => throw new NotImplementedException()
        };

        var downloadResults = await downloadTask.ConfigureAwait(false);

        await _auditService.AuditDownloadsAsync(downloadResults.ToList()
                                                               .AsReadOnly())
                           .ConfigureAwait(false);
    }

    private async Task<IEnumerable<DownloadResult>> DownloadAsync(
        IReadOnlyCollection<DownloadContext> downloadContexts,
        ProgressContext progressContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var downloadTasks = downloadContexts.Select<DownloadContext, Task<DownloadResult>>(async downloadContext =>
            {
                var downloadTask = progressContext.AddTask($"[green]{downloadContext.VideoId}[/]");

                var progress = new Progress<double>(value => downloadTask.Increment(value));

                try
                {
                    return await _youtubeService.DownloadAsync(
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
            });

            return await Task.WhenAll(downloadTasks)
                             .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync($"An error occurred while downloading data: {ex.Message}");
            throw;
        }
    }

    private async Task<IEnumerable<DownloadResult>> DownloadFromVideoLinkAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var videoId = AnsiConsoleExtensions.PromptVideoId();

        var downloadContext = getDownloadContext(videoId);

        return await AnsiConsoleExtensions.ShowProgressAsync(async ctx => await DownloadAsync([downloadContext], ctx).ConfigureAwait(false))
                                          .ConfigureAwait(false);
    }

    private async Task<IEnumerable<DownloadResult>> DownloadFromPlaylistLinkAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var playlistId = AnsiConsoleExtensions.PromptPlaylistId();

        var videoIds = await _youtubeService.GetVideoIdsFromPlaylistAsync(playlistId, cancellationToken);

        var downloadContext = videoIds.Select(getDownloadContext)
                                      .ToList()
                                      .AsReadOnly();

        return await AnsiConsoleExtensions.ShowProgressAsync(ctx => DownloadAsync(downloadContext, ctx))
                                          .ConfigureAwait(false);
    }

    private async Task<IEnumerable<DownloadResult>> DownloadFromYouTubeExportedFileAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var exportedFilePath = AnsiConsoleExtensions.PromptExportedFilePath();

        var lines = await File.ReadAllLinesAsync(exportedFilePath, cancellationToken)
                              .ConfigureAwait(false);

        var downloadContexts = lines.Skip(1)
                                   .Where(l => !string.IsNullOrWhiteSpace(l))
                                   .Select(l => getDownloadContext(GetVideoId(l)))
                                   .ToList()
                                   .AsReadOnly();

        return await AnsiConsoleExtensions.ShowProgressAsync(ctx => DownloadAsync(downloadContexts, ctx))
                                          .ConfigureAwait(false);

        static VideoId GetVideoId(string line) =>
            line.Split(',')
                .First()
                .Trim();
    }

    private async Task<IEnumerable<DownloadResult>> DownloadFromFromFailedDownloads(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken)
    {
        var emptyDownloadResults = Enumerable.Empty<DownloadResult>();
        IReadOnlyCollection<DownloadResult.Failure> failedDownloads = ReadOnlyCollection<DownloadResult.Failure>.Empty;

        try
        {
            failedDownloads = await _auditService.ListFailedDownloadsAsync(cancellationToken)
                                                 .ConfigureAwait(false);

        }
        catch (FileNotFoundException)
        {
            AnsiConsole.Write(new Markup("failed downloads [red]folder not found.[/]"));
            return emptyDownloadResults;
        }

        if (failedDownloads.Count == 0)
        {
            AnsiConsole.Write(new Markup("failed downloads [yellow]not found.[/]"));
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

        // TODO: Extension ToReadOnlyList -> ToList().AsReadOnly()
        return await AnsiConsoleExtensions.ShowProgressAsync(ctx => DownloadAsync(downloadContexts.ToList().AsReadOnly(), ctx))
                                          .ConfigureAwait(false);
    }
}