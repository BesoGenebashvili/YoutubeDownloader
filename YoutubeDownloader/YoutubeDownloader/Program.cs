using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;
using System.IO.Compression;
using Spectre.Console;

Console.WriteLine("Hello, World!");

//TODO: DI
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
                     new SpinnerColumn(),
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
                                 var fileName = await DownloadMP3Async(
                                                          youtubeClient,
                                                          videoId,
                                                          "Files",
                                                          progress)
                                                      .ConfigureAwait(false);

                                 return new DownloadResult.Success(videoId, fileName);
                             }
                             catch (Exception ex)
                             {
                                 downloadTask.StopTask();
                                 return new DownloadResult.Failure(videoId, ex.ToString());
                             }
                         });

                         var downloadResults = await Task.WhenAll(downloadTasks)
                                                         .ConfigureAwait(false);


                         // Audit
                     }
                     catch (Exception ex)
                     {
                         await Console.Out.WriteLineAsync($"FATAL ERROR: {ex}");
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

static async Task<string> DownloadMP3Async(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
    IProgress<double>? progress = null,
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

    return fileName;
}

static async Task<string> DownloadMP4Async(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
    string ffmpegPath,
    IProgress<double>? progress = null,
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

static async Task AuditSuccessAsync(DownloadResult.Success result) => throw new NotImplementedException();
static async Task AuditFailureAsync(DownloadResult.Failure result) => throw new NotImplementedException();

enum ContentType
{
    MP3,
    MP4
}

internal abstract record DownloadResult(string VideoId)
{
    internal record Success(string VideoId, string FileName) : DownloadResult(VideoId);
    internal record Failure(string VideoId, string ErrorMessage) : DownloadResult(VideoId);
}