using YoutubeExplode.Videos;

namespace YoutubeDownloader.Extensions;

public static class FileNameResolverExtensions
{
    private const string ID = "{Id}";
    private const string TITLE = "{Title}";
    private const string CHANNEL_ID = "{ChannelId}";

    public const string DEFAULT_TEMPLATE = $"{TITLE}";

    public static string ResolveFileName(this Video video, string? fileNameTemplate)
    {
        var filename = ReplacePlaceholders();

        var validFilename = FileSystemExtensions.RemoveInvalidCharactersFromFileName(filename);

        return string.IsNullOrWhiteSpace(validFilename)
                     ? video.Id
                     : validFilename;

        string ReplacePlaceholders()
        {
            string[] placeholders = [ID, TITLE, CHANNEL_ID];

            var template = !string.IsNullOrWhiteSpace(fileNameTemplate) && placeholders.Any(fileNameTemplate.Contains)
                                  ? fileNameTemplate
                                  : DEFAULT_TEMPLATE;

            return template.Replace(ID, video.Id)
                           .Replace(TITLE, video.Title)
                           .Replace(CHANNEL_ID, video.Author.ChannelId);
        }
    }
}
