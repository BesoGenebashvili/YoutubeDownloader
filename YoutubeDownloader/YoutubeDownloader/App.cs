using Spectre.Console;

namespace YoutubeDownloader;

public sealed class App(YoutubeService youtubeService, IAuditService auditService)
{
    private readonly YoutubeService youtubeService = youtubeService;
    private readonly IAuditService auditService = auditService;

    public async Task RunAsync(string[] args)
    {
        // Receive from settings/console
        var videoIds = new List<string>
    {
        "Du2TXMb1IHo",
        "r_nSu8UOYdo1",
        "bdWIwpTS48s"
    };

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
                                         var (fileName, fileSizeInMB) = await youtubeService.DownloadMP3Async(
                                                                  videoId,
                                                                  progress)
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

                                 await auditService.AuditDownloadsAsync(downloadResults).ConfigureAwait(false);
                             }
                             catch (Exception ex)
                             {
                                 await Console.Out.WriteLineAsync($"An error occurred while downloading data: {ex.Message}");
                             }
                         }).ConfigureAwait(false);
    }
}