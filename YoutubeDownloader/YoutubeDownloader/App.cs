using Spectre.Console;
using YoutubeDownloader.Models;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

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
                DownloadOption.FromYouTubeExportedFile,
                DownloadOption.FromPlaylistLink,
                DownloadOption.FromVideoLink
            ]);

        var getDownloadContext = AnsiConsoleExtensions.SelectDownloadContext();

        var downloadTask = downloadOption switch
        {
            DownloadOption.FromYouTubeExportedFile => DownloadFromYouTubeExportedFile(getDownloadContext, cancellationToken),
            DownloadOption.FromPlaylistLink => DownloadFromPlaylistLink(getDownloadContext, cancellationToken),
            DownloadOption.FromVideoLink => DownloadFromVideoLinkAsync(getDownloadContext, cancellationToken),
            _ => throw new NotImplementedException(),
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

    private Task<IEnumerable<DownloadResult>> DownloadFromYouTubeExportedFile(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    private async Task<IEnumerable<DownloadResult>> DownloadFromPlaylistLink(
        Func<VideoId, DownloadContext> getDownloadContext,
        CancellationToken cancellationToken = default)
    {
        var playlistId = AnsiConsoleExtensions.PromptPlaylistId();

        var videoIds = await _youtubeService.GetVideoIdsFromPlaylistAsync(playlistId, cancellationToken);

        var downloadContext = videoIds.Select(getDownloadContext)
                                      .ToList()
                                      .AsReadOnly();

        return await AnsiConsoleExtensions.ShowProgressAsync(async ctx => await DownloadAsync(downloadContext, ctx).ConfigureAwait(false))
                                          .ConfigureAwait(false);
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
}

public static class AnsiConsoleExtensions
{
    public static PlaylistId PromptPlaylistId() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]playlist id[/] or [green]url[/]:")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Invalid playlist id or url[/]")
                .Validate(playlistId => playlistId switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]playlist id or url cannot be empty or whitespace[/]"),
                    var v when PlaylistId.TryParse(v) is null => ValidationResult.Error("[red]Invalid playlist id or url format[/]"),
                    _ => ValidationResult.Success()
                }));

    public static VideoId PromptVideoId() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]video id[/] or [green]url[/]:")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Invalid video id or url[/]")
                .Validate(videoId => videoId switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]Video id or url cannot be empty or whitespace[/]"),
                    var v when VideoId.TryParse(v) is null => ValidationResult.Error("[red]Invalid video id or url format[/]"),
                    _ => ValidationResult.Success()
                }));

    public static DownloadOption SelectDownloadOption(DownloadOption[] options)
    {
        var optionsLookup = options.ToLookup(option => option switch
        {
            DownloadOption.FromYouTubeExportedFile => "From YouTube exported file",
            DownloadOption.FromPlaylistLink => "From playlist link",
            DownloadOption.FromVideoLink => "From video link",
            _ => throw new NotImplementedException(nameof(option))
        });

        var selectedOption =
            AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select download [green]option[/]:")
                    .AddChoices(optionsLookup.Select(x => x.Key)));

        return optionsLookup[selectedOption].First();
    }

    public static Func<VideoId, DownloadContext> SelectDownloadContext()
    {
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

        return Enum.TryParse<AudioQuality>(downloadFormatAndQuality, out var audioQuality)
                   ? (VideoId videoId) => new DownloadContext.MP3(videoId, audioQuality)
                   : (VideoId videoId) => new DownloadContext.MP4(videoId, Enum.Parse<VideoQuality>(downloadFormatAndQuality));
    }

    public static async Task<T> ShowProgressAsync<T>(Func<ProgressContext, Task<T>> action) =>
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
}