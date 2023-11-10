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
        // TODO: Custom logic for files
        RuleFor(s => s.SaveFolderPath)
            .NotEmpty();
    }
}