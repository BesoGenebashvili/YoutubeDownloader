using FluentValidation;

namespace YoutubeDownloader.Settings;

public sealed class DownloaderSettings
{
    public const string SectionName = "downloaderSettings";

    public required string SaveFolderPath { get; init; }
    public string? FFmpegPath { get; init; }
}

public sealed class DownloaderSettingsValidator : AbstractValidator<DownloaderSettings>
{
    public DownloaderSettingsValidator()
    {
        RuleFor(s => s.SaveFolderPath)
            .NotEmpty()
            .MustBeValidFolderPath();


#pragma warning disable CS8620
        // TODO: dotnet/runetime issue #36510
        When(s => s.FFmpegPath != string.Empty,
            () => RuleFor(s => s.FFmpegPath)
                      .NotEmpty()
                      .MustBeValidFilePath());
#pragma warning restore
    }
}