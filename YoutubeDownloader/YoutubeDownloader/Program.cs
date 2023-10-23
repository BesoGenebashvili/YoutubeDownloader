#pragma warning disable SYSLIB1045

using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

Console.WriteLine("Hello, World!");

var youtubeClient = new YoutubeClient();

if (!Directory.Exists("Files"))
    Directory.CreateDirectory("Files");

await DownloadAsync(youtubeClient, "ifgpiGs_4Js", "Files", ContentType.MP3);

static async Task<(bool success, string? filename, string? error)> DownloadAsync(
    YoutubeClient youtubeClient,
    VideoId videoId,
    string folderPath,
    ContentType contentType,
    CancellationToken cancellationToken = default)
{
    try
    {
        var video = await youtubeClient.Videos
                                       .GetAsync(videoId, cancellationToken)
                                       .ConfigureAwait(false);

        var streamManifest = await youtubeClient.Videos
                                                .Streams
                                                .GetManifestAsync(videoId, cancellationToken);

        var (fileExtension, streamInfo) = contentType switch
        {

            ContentType.MP3 => ("mp3", streamManifest.GetAudioOnlyStreams()
                                                     .GetWithHighestBitrate()),
            ContentType.MP4 => ("mp4", streamManifest.GetMuxedStreams()
                                                     .Where(s => s.Container == Container.Mp4)
                                                     .GetWithHighestVideoQuality()),
            _ => throw new NotImplementedException(nameof(contentType))
        };

        var fileName = ResolveFilename(video.Title, videoId);

        var filePath = Path.Combine(folderPath, $"{fileName}.{fileExtension}");

        await youtubeClient.Videos
                           .Streams
                           .DownloadAsync(streamInfo, filePath, null, cancellationToken);

        Console.WriteLine($"Downloaded: {videoId}");
        return (true, fileName, null);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {videoId}, {ex.Message}");
        return (false, null, ex.Message);
    }

    static string ResolveFilename(string title, VideoId videoId)
    {
        var newFilename = Regex.Replace(title, @"[\\\/:*?""<>|]", string.Empty);

        return string.IsNullOrWhiteSpace(newFilename)
                     ? videoId
                     : newFilename;
    }
}

enum ContentType
{
    MP3,
    MP4
}