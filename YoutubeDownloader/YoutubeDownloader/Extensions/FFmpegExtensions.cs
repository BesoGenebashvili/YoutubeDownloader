namespace YoutubeDownloader.Extensions;

public static class FFmpegExtensions
{
    public static async Task ConfigureAsync(
        string? ffmpegPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsFFmpegAvailable(ffmpegPath))
            {
                var operatingSystem = OperatingSystemExtensions.GetOperatingSystem();

                await AnsiConsoleExtensions.ShowStatusAsync(
                                               $"Downloading FFmpeg for [green]{operatingSystem}[/]",
                                               async _ => await DownloadFFmpegAsync(operatingSystem, cancellationToken).ConfigureAwait(false))
                                           .ConfigureAwait(false);

                // Log -> AnsiConsole.Clear()
                AnsiConsoleExtensions.MarkupLine("FFmpeg downloaded ", "successfully", AnsiColor.Green);
            }
        }
        catch (TaskCanceledException)
        {
            throw new TaskCanceledException("An operation cancelled while configuring the FFmpeg settings");
        }
        catch (Exception ex)
        {
            AnsiConsoleExtensions.MarkupLine("An error occurred while configuring the FFmpeg settings: ", ex.Message, AnsiColor.Red);
            throw;
        }
    }

    private static bool IsFFmpegPathValid(string? ffmpegPath) =>
        !string.IsNullOrWhiteSpace(ffmpegPath) && File.Exists(ffmpegPath);

    private static bool IsFFmpegExistInDirectory() =>
        Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory())
                 .Any(f => Path.GetFileNameWithoutExtension(f)
                               .Equals("ffmpeg", StringComparison.OrdinalIgnoreCase));

    private static bool IsFFmpegExistInEnvironmentVariables() =>
        Environment.GetEnvironmentVariable("ffmpeg") is not null;

    private static bool IsFFmpegAvailable(string? ffmpegPath) =>
        IsFFmpegPathValid(ffmpegPath) ||
        IsFFmpegExistInDirectory() ||
        IsFFmpegExistInEnvironmentVariables();

    private static async Task<string> DownloadFFmpegAsync(
        OperatingSystem operatingSystem,
        CancellationToken cancellationToken = default)
    {
        var releaseUrl = GetDownloadUrl();

        using var httpClient = new HttpClient();
        await using var stream = await httpClient.GetStreamAsync(releaseUrl, cancellationToken)
                                                 .ConfigureAwait(false);

        var ffmpegFileName = GetFFmpegFileName(operatingSystem);
        var ffmpegFilePath = Path.Combine(Environment.CurrentDirectory, ffmpegFileName);

        await stream.UnpackFromZipInFileAsync(
                        ffmpegFileName,
                        ffmpegFilePath,
                        cancellationToken)
                    .ConfigureAwait(false);

        return ffmpegFilePath;

        string GetFFmpegFileName(OperatingSystem operatingSystem) => operatingSystem switch
        {
            OperatingSystem.Windows => "ffmpeg.exe",
            OperatingSystem.Linux or OperatingSystem.MacOS => "ffmpeg",
            _ => throw new NotImplementedException(nameof(operatingSystem)),
        };

        string GetDownloadUrl() => (operatingSystem, Environment.Is64BitOperatingSystem) switch
        {
            (OperatingSystem.Windows, true) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-win-64.zip",
            (OperatingSystem.Windows, false) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.2.1/ffmpeg-4.2.1-win-32.zip",
            (OperatingSystem.Linux, true) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-linux-64.zip",
            (OperatingSystem.Linux, false) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-linux-32.zip",
            (OperatingSystem.MacOS, _) => "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-osx-64.zip",
            _ => throw new NotImplementedException(nameof(operatingSystem)),
        };
    }
}