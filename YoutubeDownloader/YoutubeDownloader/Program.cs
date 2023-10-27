#pragma warning disable SYSLIB1045

using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;
using System.IO.Compression;

Console.WriteLine("Hello, World!");

static string ResolveFilename(string title, VideoId videoId)
{
    // TODO: we can use `Path.GetInvalidFileNameChars()` & `Path.GetInvalidPathChars`

    var newFilename = Regex.Replace(title, @"[\\\/:*?""<>|]", string.Empty);

    return string.IsNullOrWhiteSpace(newFilename)
                 ? videoId
                 : newFilename;
}

static async Task<string> DownloadMP3Async(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
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

    // TODO: report progress
    await youtubeClient.Videos
                       .Streams
                       .DownloadAsync(
                            streamInfo,
                            filePath,
                            cancellationToken: cancellationToken)
                       .ConfigureAwait(false);

    return fileName;
}

static async Task<string> DownloadMP4Async(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
    string ffmpegPath,
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
                            cancellationToken: cancellationToken)
                       .ConfigureAwait(false);

    return fileName;
}

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

enum ContentType
{
    MP3,
    MP4
}