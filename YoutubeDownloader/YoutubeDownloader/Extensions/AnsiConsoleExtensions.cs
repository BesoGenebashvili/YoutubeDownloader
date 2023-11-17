using Spectre.Console;
using YoutubeDownloader.Models;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Extensions;

public enum AnsiColor
{
    Green,
    Gray,
    Yellow,
    Red
}

public static class AnsiConsoleExtensions
{
    public static void MarkupLine(
        string text,
        string? textWithMarkup = null,
        AnsiColor markupColor = AnsiColor.Gray)
    {
        if (textWithMarkup is { Length: > 0 })
        {
            AnsiConsole.MarkupLine($"{text}[{markupColor}]{textWithMarkup}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine(text);
        }
    }

    public static FailedDownloadResendSetting SelectFailedDownloadResendSettings(FailedDownloadResendSetting[] settings)
    {
        var settingsLookup = settings.ToLookup(setting => setting switch
        {
            FailedDownloadResendSetting.KeepOriginal => "Keep original",
            FailedDownloadResendSetting.OverrideWithNew => "Override with new",
            _ => throw new NotImplementedException(nameof(setting))
        });

        var selectedOption =
            AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select download [green]format & quality[/] setting:")
                    .AddChoices(settingsLookup.Select(x => x.Key)));

        return settingsLookup[selectedOption].First();
    }

    public static string PromptExportedFilePath() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]exported file[/] path:")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Invalid exported file path[/]")
                .Validate(exportedFilePath => exportedFilePath switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]exported file path cannot be empty or whitespace[/]"),
                    var v when !File.Exists(v) => ValidationResult.Error("[red]exported file path should exist on this device[/]"),
                    _ => ValidationResult.Success()
                }));

    public static PlaylistId PromptPlaylistId() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]playlist id[/] or [green]url[/]:")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Invalid playlist id or url[/]")
                .Validate(playlistId => playlistId switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]playlist id or url cannot be empty or whitespace[/]"),
                    var v when PlaylistId.TryParse(v) is null => ValidationResult.Error("[red]Invalid playlist id or url format[/]"),
                    _ => ValidationResult.Success()
                }));

    public static VideoId PromptVideoId() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]video id[/] or [green]url[/]:")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Invalid video id or url[/]")
                .Validate(videoId => videoId switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]Video id or url cannot be empty or whitespace[/]"),
                    var v when VideoId.TryParse(v) is null => ValidationResult.Error("[red]Invalid video id or url format[/]"),
                    _ => ValidationResult.Success()
                }));

    public static DownloadOption SelectDownloadOption(DownloadOption[] options)
    {
        var optionsLookup = options.ToLookup(option => option switch
        {
            DownloadOption.FromVideoLink => "From video link",
            DownloadOption.FromPlaylistLink => "From playlist link",
            DownloadOption.FromYouTubeExportedFile => "From YouTube exported file",
            DownloadOption.FromFailedDownloads => "From failed downloads",
            _ => throw new NotImplementedException(nameof(option))
        });

        var selectedOption =
            AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select download [green]option[/]:")
                    .AddChoices(optionsLookup.Select(x => x.Key)));

        return optionsLookup[selectedOption].First();
    }

    public static Func<VideoId, DownloadContext> SelectDownloadContext()
    {
        var downloadFormatAndQuality = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select download [green]format & quality[/]:")
                .AddChoiceGroup(
                    FileFormat.MP3.ToString(),
                    [
                        AudioQuality.LowBitrate.ToString(),
                        AudioQuality.HighBitrate.ToString()
                    ])
                .AddChoiceGroup(
                    FileFormat.MP4.ToString(),
                    [
                        VideoQuality.SD.ToString(),
                        VideoQuality.HD.ToString(),
                        VideoQuality.FullHD.ToString()
                    ]));

        return Enum.TryParse<AudioQuality>(downloadFormatAndQuality, out var audioQuality)
                   ? (VideoId videoId) => new DownloadContext(videoId, new VideoConfiguration.MP3(audioQuality))
                   : (VideoId videoId) => new DownloadContext(videoId, new VideoConfiguration.MP4(Enum.Parse<VideoQuality>(downloadFormatAndQuality)));
    }

    public static async Task<T> ShowProgressAsync<T>(Func<ProgressContext, Task<T>> action) =>
        await AnsiConsole.Progress()
                         .AutoRefresh(true)
                         .AutoClear(true)
                         .HideCompleted(true)
                         .Columns(
                         [
                             new TaskDescriptionColumn(),
                             new ProgressBarColumn(),
                             new PercentageColumn(),
                             new RemainingTimeColumn(),
                             new SpinnerColumn(Spinner.Known.Point),
                         ])
                         .StartAsync(action)
                         .ConfigureAwait(false);
}