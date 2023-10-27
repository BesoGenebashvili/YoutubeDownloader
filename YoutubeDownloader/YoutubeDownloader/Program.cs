#pragma warning disable SYSLIB1045

using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;

Console.WriteLine("Hello, World!");

var youtubeClient = new YoutubeClient();
var ffmpgPath = "ffmpeg.exe";

if (!Directory.Exists("Files"))
    Directory.CreateDirectory("Files");

await DownloadMP4Async(youtubeClient, "Z3m7HXeiHpg", "Files", ffmpgPath);

static string ResolveFilename(string title, VideoId videoId)
{
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

    var streamInfos = new IStreamInfo[] { audioStreamInfo, videoStreamInfo };

    // TODO: report progress
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

enum ContentType
{
    MP3,
    MP4
}