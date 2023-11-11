using Spectre.Console;

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

        // TODO: multi-select prompt
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

        // TODO switch
        var (fileFormat, audioQuality, videoQuality) = ParseFileFormatAndQuality(downloadFormatAndQuality);

        var downloadResults = downloadOption switch
        {
            FromYouTubeExportedFile => DownloadFromYouTubeExportedFile(),
            FromPlaylistLink => DownloadFromPlaylistLink(),
            FromVideoLink => DownloadFromVideoLink(),
            _ => throw new NotImplementedException(),
        };

        // temporary code for testing purposes
        string[] videoIds = [
            "Du2TXMb1IHo",
            "r_nSu8UOYdo1",
            "bdWIwpTS48s"
        ];

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
                         .StartAsync(async ctx =>
                         {
                             try
                             {
                                 var downloadTasks = videoIds.Select((Func<string, Task<DownloadResult>>)(async videoId =>
                                 {

                                     var downloadTask = ctx.AddTask($"[green]{videoId}[/]");

                                     var progress = new Progress<double>(value => downloadTask.Increment(value));

                                     try
                                     {
                                         var (fileName, fileSizeInMB) = await _youtubeService.DownloadMP3Async(
                                                                  videoId,
                                                                  progress: progress)
                                                              .ConfigureAwait(false);

                                         return new DownloadResult.Success(
                                                        videoId,
                                                        fileName,
                                                        FileFormat.MP3,
                                                        DateTime.Now,
                                                        fileSizeInMB);
                                     }
                                     catch (Exception ex)
                                     {
                                         downloadTask.StopTask();

                                         return new DownloadResult.Failure(
                                                        videoId,
                                                        FileFormat.MP3,
                                                        DateTime.Now,
                                                        ex.Message
                                                          .Replace(',', '.'));
                                     }
                                 }));

                                 var downloadResults = await Task.WhenAll(downloadTasks)
                                                                 .ConfigureAwait(false);

                                 await _auditService.AuditDownloadsAsync(downloadResults).ConfigureAwait(false);
                             }
                             catch (Exception ex)
                             {
                                 await Console.Out.WriteLineAsync($"An error occurred while downloading data: {ex.Message}");
                             }
                         }).ConfigureAwait(false);

        // TODO: implement Either<TLeft, TRight>
        (FileFormat fileFormat, AudioQuality? audioQuality, VideoQuality? videoQuality) ParseFileFormatAndQuality(string s) =>
            Enum.TryParse<AudioQuality>(s, out var audioQuality)
                ? (FileFormat.MP3, audioQuality, null)
                : (FileFormat.MP4, null, Enum.Parse<VideoQuality>(s));
    }

    private object DownloadFromVideoLink() => throw new NotImplementedException();
    private object DownloadFromPlaylistLink() => throw new NotImplementedException();
    private object DownloadFromYouTubeExportedFile() => throw new NotImplementedException();
}