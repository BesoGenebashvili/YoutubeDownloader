using System.IO.Compression;

namespace YoutubeDownloader;

public static class FFmpegExtensions
{
    // bool throwIfNotConfigured ?
    public static async void Configure(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsFFmpegAvailable())
            {
                await DownloadFFmpegAsync(cancellationToken).ConfigureAwait(false);
                await Console.Out.WriteLineAsync("FFmpeg downloaded successfully");
            }
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync($"An error occurred while configuring the FFmpeg settings: {ex.Message}");
            // TODO: Log.Error
        }
    }

    private static string GetFFmpegFileName() =>
        OperatingSystem.IsWindows()
                       ? "ffmpeg.exe"
                       : "ffmpeg";

    private static bool IsFFmpegAvailable() =>
        File.Exists(GetFFmpegFileName()) ||
        Environment.GetEnvironmentVariable("ffmpeg") is not null;

    private static async Task DownloadFFmpegAsync(CancellationToken cancellationToken = default)
    {
        var releaseUrl = GetDownloadUrl();

        using var httpClient = new HttpClient();
        await using var stream = await httpClient.GetStreamAsync(releaseUrl, cancellationToken)
                                                 .ConfigureAwait(false);

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        var ffmpegFileName = GetFFmpegFileName();

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
}