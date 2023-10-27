#pragma warning disable SYSLIB1045

using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;

Console.WriteLine("Hello, World!");

var youtubeClient = new YoutubeClient();

if (!Directory.Exists("Files"))
    Directory.CreateDirectory("Files");

var videoIds = File.ReadAllLines("Favorites-videos.csv")
                   .Skip(1)
                   .Where(t => !string.IsNullOrWhiteSpace(t))
                   .Select(t => t.Split(',')
                                 .First()
                                 .Trim())
                   .ToArray();

foreach (var videoId in videoIds)
{
    await DownloadMP3Async(youtubeClient, videoId, "Files");
}

static string ResolveFilename(string title, VideoId videoId)
{
    var newFilename = Regex.Replace(title, @"[\\\/:*?""<>|]", string.Empty);

    return string.IsNullOrWhiteSpace(newFilename)
                 ? videoId
                 : newFilename;
}

/// Returns: fileName
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
                                            .GetManifestAsync(videoId, cancellationToken);

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
                            cancellationToken: cancellationToken);

    return fileName;
}

/// Returns: fileName
static async Task<string> DownloadMP4Async(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
    CancellationToken cancellationToken = default)
{
    throw new NotImplementedException();
}

enum ContentType
{
    MP3,
    MP4
}