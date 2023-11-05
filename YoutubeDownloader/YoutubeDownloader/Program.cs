using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;
using System.IO.Compression;
using Spectre.Console;
using System.Text;
using System.Runtime.InteropServices;
using static DownloadResult;

Console.WriteLine("Hello, World!");

ConfigureConsole();

var youtubeClient = new YoutubeClient();

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
                         var downloadTasks = videoIds.Select<string, Task<DownloadResult>>(async videoId =>
                         {

                             var downloadTask = ctx.AddTask($"[green]{videoId}[/]");

                             var progress = new Progress<double>(value => downloadTask.Increment(value));

                             try
                             {
                                 var (fileName, fileSizeInMB) = await DownloadMP3Async(
                                                          youtubeClient,
                                                          videoId,
                                                          "Files",
                                                          progress)
                                                      .ConfigureAwait(false);

                                 return new Success(
                                                videoId,
                                                fileName,
                                                FileFormat.MP3,
                                                fileSizeInMB);
                             }
                             catch (Exception ex)
                             {
                                 downloadTask.StopTask();

                                 return new Failure(
                                                videoId,
                                                FileFormat.MP3,
                                                ex.Message
                                                  .Replace(',', '.'));
                             }
                         });

                         var downloadResults = await Task.WhenAll(downloadTasks)
                                                         .ConfigureAwait(false);

                         await AuditDownloadsAsync(downloadResults).ConfigureAwait(false);
                     }
                     catch (Exception ex)
                     {
                         await Console.Out.WriteLineAsync($"An error occurred while downloading data: {ex.Message}");
                     }
                 }).ConfigureAwait(false);

Console.ReadLine();

static string ResolveFilename(string title, VideoId videoId)
{
    var regexPattern = $"[{Regex.Escape(new(Path.GetInvalidFileNameChars()))}]";

    var newFilename = Regex.Replace(title, regexPattern, string.Empty);

    return string.IsNullOrWhiteSpace(newFilename)
                 ? videoId
                 : newFilename;
}

static async Task<(string fileName, double fileSizeInMB)> DownloadMP3Async(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
    IProgress<double>? progress = default,
    CancellationToken cancellationToken = default)
{
    var video = await youtubeClient.Videos
                                   .GetAsync(videoId, cancellationToken)
                                   .ConfigureAwait(false);

    var streamManifest = await youtubeClient.Videos
                                            .Streams
                                            .GetManifestAsync(videoId, cancellationToken)
                                            .ConfigureAwait(false);

    var streamInfo = streamManifest.GetAudioOnlyStreams()
                                   .GetWithHighestBitrate();

    var fileName = ResolveFilename(video.Title, videoId);
    var filePath = Path.Combine(folderPath, $"{fileName}.mp3");

    await youtubeClient.Videos
                       .Streams
                       .DownloadAsync(
                            streamInfo,
                            filePath,
                            progress,
                            cancellationToken: cancellationToken)
                       .ConfigureAwait(false);

    return (fileName, streamInfo.Size.MegaBytes);
}

static async Task<string> DownloadMP4Async(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
    string? ffmpegPath = default,
    IProgress<double>? progress = default,
    CancellationToken cancellationToken = default)
{
    var video = await youtubeClient.Videos
                                   .GetAsync(videoId, cancellationToken)
                                   .ConfigureAwait(false);

    var streamManifest = await youtubeClient.Videos
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
    var filePath = Path.Combine(folderPath, $"{fileName}.mp4");

    var streamInfos = new IStreamInfo[]
    {
        audioStreamInfo,
        videoStreamInfo
    };

    await youtubeClient.Videos
                       .DownloadAsync(
                            streamInfos,
                            new ConversionRequestBuilder(filePath)
                                    .SetFFmpegPath(ffmpegPath)
                                    .Build(),
                            progress,
                            cancellationToken: cancellationToken)
                       .ConfigureAwait(false);

    return fileName;
}

// TODO: relative path | environment variable
static async Task<bool> IsFFmpegAvailableAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

static async Task DownloadFFmpegAsync(CancellationToken cancellationToken = default)
{
    var releaseUrl = GetDownloadUrl();

    using var httpClient = new HttpClient();
    await using var stream = await httpClient.GetStreamAsync(releaseUrl, cancellationToken)
                                             .ConfigureAwait(false);

    using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

    var ffmpegFileName = OperatingSystem.IsWindows()
                                        ? "ffmpeg.exe"
                                        : "ffmpeg";

    var ffmpegFilePath = Path.Combine(Environment.CurrentDirectory, ffmpegFileName);

    var zipEntry = zip.GetEntry(ffmpegFileName)
                      ?? throw new FileNotFoundException($"{ffmpegFileName} not found in {Environment.CurrentDirectory}");

    await using var zipEntryStream = zipEntry.Open();
    await using var fileStream = File.Create(ffmpegFilePath);

    await zipEntryStream.CopyToAsync(fileStream, cancellationToken);

    static string GetDownloadUrl()
    {
        var is64BitOS = Environment.Is64BitOperatingSystem;

        if (OperatingSystem.IsWindows())
        {
            return is64BitOS ? "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-win-64.zip"
                             : "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.2.1/ffmpeg-4.2.1-win-32.zip";
        }
        else if (OperatingSystem.IsLinux())
        {
            return is64BitOS ? "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-linux-64.zip"
                             : "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-linux-32.zip";
        }

        throw new NotImplementedException("Unknown IOS");
    }
}

static async Task AuditDownloadsAsync(
    IReadOnlyCollection<DownloadResult> downloads,
    CancellationToken cancellationToken = default)
{
    var successes = downloads.OfType<Success>()
                             .ToList()
                             .AsReadOnly();

    var failures = downloads.OfType<Failure>()
                            .ToList()
                            .AsReadOnly();

    Task[] auditTasks =
    [
        AuditSuccessfulDownloadsAsync(successes, cancellationToken),
        AuditFailedDownloadsAsync(failures, cancellationToken)
    ];

    await Task.WhenAll(auditTasks)
              .ConfigureAwait(false);
}

static async Task AuditSuccessfulDownloadsAsync(
    IReadOnlyCollection<Success> successes,
    CancellationToken cancellationToken = default)
{
    // TODO: "File Absolute Path"
    var headers = string.Join(',', ["Video Id", "File Name", "File Format", "File Size In MB"]);
    var records = successes.Select(Extensions.AsCSVColumn);

    // TODO: Receive as parameter
    var fileName = "Succeed.csv";

    var content = File.Exists(fileName)
                      ? records
                      : records.Prepend(headers);

    await File.AppendAllLinesAsync(
                  fileName,
                  content,
                  cancellationToken)
              .ConfigureAwait(false);
}

static async Task AuditFailedDownloadsAsync(
    IReadOnlyCollection<Failure> failures,
    CancellationToken cancellationToken = default)
{
    var headers = string.Join(',', ["Video Id", "File Format", "Retry Count", "Error Message"]);

    // TODO: Receive as parameter
    var fileName = "Failed.csv";

    if (!File.Exists(fileName))
    {
        await File.WriteAllLinesAsync(
                      fileName,
                      failures.Select(Extensions.AsCSVColumn)
                              .Prepend(headers),
                      cancellationToken)
                  .ConfigureAwait(false);

        return;
    }

    var oldRecords = File.ReadAllLines(fileName)
                         .Skip(1)
                         .Select(s =>
                         {
                             var columns = s.Split(',');
                         
                             var fileFormat = Enum.Parse<FileFormat>(columns[1], true);
                         
                             return (failure: new Failure(columns[0], fileFormat, columns[3]),
                                     retryCount: int.Parse(columns[2]));
                         });

    var newRecords = oldRecords.GroupJoin(
                        failures,
                        x => (x.failure.VideoId, x.failure.FileFormat),
                        f => (f.VideoId, f.FileFormat),
                        (x, xs) =>
                            (failure: x.failure with { ErrorMessage = xs.Last().ErrorMessage },
                             retryCount: x.retryCount + xs.Count()));


    await File.WriteAllLinesAsync(
                  fileName,
                  newRecords.Select(x => x.failure.AsCSVColumn(x.retryCount))
                            .Prepend(headers),
                  cancellationToken)
              .ConfigureAwait(false);
}

unsafe static void ConfigureConsole()
{
    try
    {
        var consoleWindow = ConsoleInterop.GetSystemMenu(ConsoleInterop.GetConsoleWindow(), false);

#pragma warning disable CA1806
        ConsoleInterop.DeleteMenu(consoleWindow, ConsoleInterop.SC_MINIMIZE, ConsoleInterop.MF_BYCOMMAND);
        ConsoleInterop.DeleteMenu(consoleWindow, ConsoleInterop.SC_MAXIMIZE, ConsoleInterop.MF_BYCOMMAND);
        ConsoleInterop.DeleteMenu(consoleWindow, ConsoleInterop.SC_SIZE, ConsoleInterop.MF_BYCOMMAND);
#pragma warning restore

        Console.WindowHeight = Console.LargestWindowHeight / 2;
        Console.WindowWidth = Console.LargestWindowWidth / 2;

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while configuring the console settings: {ex.Message}");
        // TODO: Log.Error
    }
}

public enum FileFormat : byte
{
    MP3,
    MP4
}

internal abstract record DownloadResult(string VideoId, FileFormat FileFormat)
{
    internal record Success(string VideoId, string FileName, FileFormat FileFormat, double FileSizeInMB) : DownloadResult(VideoId, FileFormat);
    internal record Failure(string VideoId, FileFormat FileFormat, string ErrorMessage) : DownloadResult(VideoId, FileFormat);
}

internal static class Extensions
{
    public static string AsCSVColumn(this Success self) =>
        $"{self.VideoId},{self.FileName},{self.FileFormat.ToString().ToLower()},{self.FileSizeInMB:F2}";

    public static string AsCSVColumn(this Failure self, int retryCount = 0) =>
        $"{self.VideoId},{self.FileFormat.ToString().ToLower()},{retryCount},{self.ErrorMessage},";
}

internal static unsafe partial class ConsoleInterop
{
    public const int MF_BYCOMMAND = 0x00000000;
    public const int SC_MINIMIZE = 0xF020;
    public const int SC_MAXIMIZE = 0xF030;
    public const int SC_SIZE = 0xF000;

    [LibraryImport("user32.dll")]
    public static partial int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

    [LibraryImport("kernel32.dll")]
    public static partial IntPtr GetConsoleWindow();
}