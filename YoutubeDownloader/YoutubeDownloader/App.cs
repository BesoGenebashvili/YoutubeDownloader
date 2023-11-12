using Spectre.Console;
using YoutubeDownloader.Models;
using YoutubeExplode.Videos;

namespace YoutubeDownloader;

public sealed class App(YoutubeService youtubeService, IAuditService auditService)
{
    private readonly YoutubeService _youtubeService = youtubeService;
    private readonly IAuditService _auditService = auditService;

    public async Task RunAsync(string[] args)
    {
        const string FromYouTubeExportedFile = "From YouTube exported file";
        const string FromPlaylistLink = "From playlist link";
        const string FromVideoLink = "From video link";

        var downloadOption = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select download [green]option[/]:")
                .AddChoices(
                [
                    FromYouTubeExportedFile,
                    FromPlaylistLink,
                    FromVideoLink
                ]));

        var downloadFormatAndQuality = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select download [green]format & quality[/]:")
                .AddChoiceGroup(
                    FileFormat.MP3.ToString(),
                    [
                        AudioQuality.LowBitrate.ToString(),
                        AudioQuality.HighBitrate.ToString()
                    ])
                .AddChoiceGroup(
                    FileFormat.MP4.ToString(),
                    [
                        VideoQuality.SD.ToString(),
                        VideoQuality.HD.ToString(),
                        VideoQuality.FullHD.ToString()
                    ]));

        var getDownloadContext = ParseDownloadContext(downloadFormatAndQuality);

        var downloadResultsTask = downloadOption switch
        {
            FromYouTubeExportedFile => DownloadFromYouTubeExportedFile(null),
            FromPlaylistLink => DownloadFromPlaylistLink(null),
            FromVideoLink => DownloadFromVideoLinkAsync(getDownloadContext),
            _ => throw new NotImplementedException(),
        };

        var downloadResults = await downloadResultsTask;

        await _auditService.AuditDownloadsAsync(downloadResults.ToList()
                                                               .AsReadOnly())
                           .ConfigureAwait(false);

        static Func<VideoId, DownloadContext> ParseDownloadContext(string s) =>
            Enum.TryParse<AudioQuality>(s, out var audioQuality)
                ? (VideoId videoId) => new DownloadContext.MP3(videoId, audioQuality)
                : (VideoId videoId) => new DownloadContext.MP4(videoId, Enum.Parse<VideoQuality>(s));
    }

    private async Task<IEnumerable<DownloadResult>> ShowProgressAsync(
        Func<ProgressContext, Task<IEnumerable<DownloadResult>>> action) =>
        await AnsiConsole.Progress()
                         .AutoRefresh(true)
                         .AutoClear(false)
                         .HideCompleted(true)
                         .Columns(
                         [
                             new TaskDescriptionColumn(),
                             new ProgressBarColumn(),
                             new PercentageColumn(),
                             new RemainingTimeColumn(),
                             new SpinnerColumn(Spinner.Known.Point),
                         ])
                         .StartAsync(action)
                         .ConfigureAwait(false);

    private async Task<IEnumerable<DownloadResult>> DownloadFromVideoLinkAsync(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var videoId = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]video id[/] or [green]url[/]:")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Invalid video id or url[/]")
                .Validate(videoId => videoId switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]Video id or url cannot be empty or whitespace[/]"),
                    var v when VideoId.TryParse(v) is null => ValidationResult.Error("[red]Invalid video id or url format[/]"),
                    _ => ValidationResult.Success()
                }));

        var downloadContext = getDownloadContext(videoId);
        return await ShowProgressAsync(async ctx => await DownloadAsync([downloadContext], ctx)).ConfigureAwait(false);
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
                var fileFormat = downloadContext switch
                {
                    DownloadContext.MP3 => FileFormat.MP3,
                    DownloadContext.MP4 => FileFormat.MP4,
                    _ => throw new NotImplementedException(nameof(downloadContext))
                };

                var downloadTask = progressContext.AddTask($"[green]{downloadContext.VideoId}[/]");

                var progress = new Progress<double>(value => downloadTask.Increment(value));

                try
                {
                    var (fileName, fileSizeInMB) = await _youtubeService.DownloadAsync(
                                                                            downloadContext,
                                                                            progress,
                                                                            cancellationToken)
                                                                        .ConfigureAwait(false);

                    return downloadContext.Success(fileName, fileSizeInMB);
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

    private Task<IEnumerable<DownloadResult>> DownloadFromPlaylistLink(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    private Task<IEnumerable<DownloadResult>> DownloadFromYouTubeExportedFile(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();
}