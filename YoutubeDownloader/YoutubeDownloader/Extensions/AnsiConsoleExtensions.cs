using Spectre.Console;
using YoutubeDownloader.Models;
using YoutubeExplode.Channels;
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

    public static IEnumerable<VideoId> SelectVideoIdsFromTitles(IEnumerable<(string title, VideoId videoId)> playlistVideos)
    {
        var markupRemovedTitles = playlistVideos.Select(x => (title: x.title.RemoveMarkup(), x.videoId));

        var selectedVideoTitles = AnsiConsole.Prompt(
                                      new MultiSelectionPrompt<string>()
                                          .Title("Select [green]video titles[/] to download:")
                                          .PageSize(10)
                                          .MoreChoicesText("[grey](Move up and down to reveal more video ids)[/]")
                                          .InstructionsText(
                                              "[grey](Press [blue]<space>[/] to toggle a video id, " +
                                              "[green]<enter>[/] to accept)[/]")
                                          .AddChoiceGroup(
                                              "Select All",
                                              markupRemovedTitles.Select(x => x.title)));

        return markupRemovedTitles.IntersectBy(
                                      selectedVideoTitles.Where(t => t is not "Select All"),
                                      v => v.title)
                                  .Select(x => x.videoId);
    }

    public static IEnumerable<VideoId> SelectVideoIds(IEnumerable<VideoId> videoIds)
    {
        var selectedVideoIds = AnsiConsole.Prompt(
                                   new MultiSelectionPrompt<string>()
                                       .Title("Select [green]video ids[/] to download:")
                                       .PageSize(10)
                                       .MoreChoicesText("[grey](Move up and down to reveal more video ids)[/]")
                                       .InstructionsText(
                                           "[grey](Press [blue]<space>[/] to toggle a video id, " +
                                           "[green]<enter>[/] to accept)[/]")
                                       .AddChoiceGroup(
                                           "Select All",
                                           videoIds.Select(v => v.ToString())));

        return selectedVideoIds.Where(x => x is not "Select All")
                               .Select(VideoId.Parse);
    }

    public static string PromptExportedFilePath() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]exported file[/] path:")
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
                .ValidationErrorMessage("[red]Invalid playlist id or url[/]")
                .Validate(playlistId => playlistId switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]playlist id or url cannot be empty or whitespace[/]"),
                    var v when PlaylistId.TryParse(v) is null => ValidationResult.Error("[red]Invalid playlist id or url format[/]"),
                    _ => ValidationResult.Success()
                }));

    public static ChannelId PromptChannelId() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]channel id[/] or [green]url[/]:")
                .ValidationErrorMessage("[red]Invalid channel id or url[/]")
                .Validate(channelId => channelId switch
                {
                    var v when string.IsNullOrWhiteSpace(v) => ValidationResult.Error("[red]channel id or url cannot be empty or whitespace[/]"),
                    var v when ChannelId.TryParse(v) is null => ValidationResult.Error("[red]Invalid channel id or url format[/]"),
                    _ => ValidationResult.Success()
                }));

    public static VideoId PromptVideoId() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]video id[/] or [green]url[/]:")
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
            DownloadOption.FromChannelUploads => "From channel uploads",
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
                .Title("Select download [green]format[/] & [green]quality[/]:")
                .AddChoiceGroup(
                    FileFormat.MP3.ToString(),
                    [
                        AudioQuality.LowBitrate.ToString(),
                        AudioQuality.HighBitrate.ToString()
                    ])
                .AddChoiceGroup(
                    FileFormat.MP4.ToString(),
                    [
                        $"{VideoQuality.SD} [gray](480p)[/]",
                        $"{VideoQuality.HD} [gray](720p)[/]",
                        $"{VideoQuality.FullHD} [gray](1080p)[/]"
                    ]));

        var formatAndQuality = downloadFormatAndQuality.Split(' ')
                                                       .First();

        return Enum.TryParse<AudioQuality>(formatAndQuality, out var audioQuality)
                   ? (VideoId videoId) => new(videoId, new VideoConfiguration.MP3(audioQuality))
                   : (VideoId videoId) => new(videoId, new VideoConfiguration.MP4(Enum.Parse<VideoQuality>(formatAndQuality)));
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

    public static async Task<T> ShowStatusAsync<T>(string message, Func<StatusContext, Task<T>> action) =>
        await AnsiConsole.Status()
                         .Spinner(Spinner.Known.Dots2)
                         .StartAsync(message, action)
                         .ConfigureAwait(false);
}