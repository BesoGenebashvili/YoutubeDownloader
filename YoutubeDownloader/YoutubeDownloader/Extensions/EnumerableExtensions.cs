namespace YoutubeDownloader.Extensions;

public static class EnumerableExtensions
{
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> self) =>
        self.ToList()
            .AsReadOnly();
}
