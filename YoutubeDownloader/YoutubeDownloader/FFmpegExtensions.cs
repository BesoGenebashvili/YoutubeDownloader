using System.IO.Compression;

namespace YoutubeDownloader;

internal enum OperatingSystem : byte
{
    Windows,
    Linux
}

public static class FFmpegExtensions
{
    public static async Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsFFmpegAvailable())
            {
                var operatingSystem = System.OperatingSystem.IsWindows()
                                          ? OperatingSystem.Windows
                                          : System.OperatingSystem.IsLinux()
                                                ? OperatingSystem.Linux
                                                : throw new ApplicationException("OS not supported");

                await DownloadFFmpegAsync(operatingSystem, cancellationToken).ConfigureAwait(false);

                await Console.Out.WriteLineAsync("FFmpeg downloaded successfully");
            }
        }
        catch (TaskCanceledException)
        {
            throw new TaskCanceledException("An operation cancelled while configuring the FFmpeg settings");
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync($"An error occurred while configuring the FFmpeg settings: {ex.Message}");
            throw;
        }
    }

    private static bool IsFFmpegExistInDirectory() =>
        Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory())
                 .Any(f => Path.GetFileNameWithoutExtension(f)
                               .Equals("ffmpeg", StringComparison.OrdinalIgnoreCase));

    private static bool IsFFmpegExistInEnvironmentVariables() =>
        Environment.GetEnvironmentVariable("ffmpeg") is not null;

    private static bool IsFFmpegAvailable() => IsFFmpegExistInDirectory() || IsFFmpegExistInEnvironmentVariables();

    private static async Task DownloadFFmpegAsync(
        OperatingSystem operatingSystem,
        CancellationToken cancellationToken = default)
    {
        var releaseUrl = GetDownloadUrl();

        using var httpClient = new HttpClient();
        await using var stream = await httpClient.GetStreamAsync(releaseUrl, cancellationToken)
                                                 .ConfigureAwait(false);

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        var ffmpegFileName = GetFFmpegFileName(operatingSystem);

        var ffmpegFilePath = Path.Combine(Environment.CurrentDirectory, ffmpegFileName);

        var zipEntry = zip.GetEntry(ffmpegFileName)
                          ?? throw new FileNotFoundException($"{ffmpegFileName} not found in {Environment.CurrentDirectory}");

        await using var zipEntryStream = zipEntry.Open();
        await using var fileStream = File.Create(ffmpegFilePath);

        await zipEntryStream.CopyToAsync(fileStream, cancellationToken);

        string GetFFmpegFileName(OperatingSystem operatingSystem) => operatingSystem switch
        {
            OperatingSystem.Windows => "ffmpeg.exe",
            OperatingSystem.Linux => "ffmpeg",
            _ => throw new NotImplementedException(nameof(operatingSystem)),
        };

        string GetDownloadUrl() => (operatingSystem, Environment.Is64BitOperatingSystem) switch
        {
            (OperatingSystem.Windows, true) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-win-64.zip",
            (OperatingSystem.Windows, false) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.2.1/ffmpeg-4.2.1-win-32.zip",
            (OperatingSystem.Linux, true) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-linux-64.zip",
            (OperatingSystem.Linux, false) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-linux-32.zip",
            _ => throw new NotImplementedException(nameof(operatingSystem)),
        };
    }
}