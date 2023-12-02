using Spectre.Console;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services;
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
                DownloadOption.FromChannelUploads,
                DownloadOption.FromYouTubeExportedFile,
                DownloadOption.FromFailedDownloads
            ]);

        var getDownloadContext = AnsiConsoleExtensions.SelectDownloadContext();

        var downloadTask = downloadOption switch
        {
            DownloadOption.FromVideoLink => _youtubeService.DownloadFromVideoLinkAsync(
                                                               getDownloadContext,
                                                               cancellationToken),

            DownloadOption.FromPlaylistLink => _youtubeService.DownloadFromPlaylistLinkAsync(
                                                                  getDownloadContext,
                                                                  cancellationToken),

            DownloadOption.FromChannelUploads => _youtubeService.DownloadFromChannelUploadsAsync(
                                                                    getDownloadContext,
                                                                    cancellationToken),

            DownloadOption.FromYouTubeExportedFile => _youtubeService.DownloadFromYouTubeExportedFileAsync(
                                                                         getDownloadContext,
                                                                         cancellationToken),

            DownloadOption.FromFailedDownloads => _youtubeService.DownloadFromFromFailedDownloadsAsync(
                                                                     getDownloadContext,
                                                                     _auditService.ListFailedDownloadsAsync,
                                                                     cancellationToken),

            _ => throw new NotImplementedException(nameof(downloadOption))
        };

        var downloadResults = await downloadTask.ConfigureAwait(false);

        await _auditService.AuditDownloadsAsync(downloadResults)
                           .ConfigureAwait(false);

        var appBehavior = AnsiConsoleExtensions.SelectAppBehavior([AppBehavior.Redownload, AppBehavior.Exist]);

        AnsiConsole.Clear();

        switch (appBehavior)
        {
            case AppBehavior.Redownload:
                await RunAsync(args).ConfigureAwait(false);
                break;

            case AppBehavior.Exist:
                AnsiConsoleExtensions.MarkupLine(string.Empty, "Bye!", AnsiColor.Green);
                await Task.Delay(TimeSpan.FromSeconds(1))
                          .ConfigureAwait(false);
                break;

            default:
                throw new NotImplementedException(nameof(appBehavior));
        }
    }
}