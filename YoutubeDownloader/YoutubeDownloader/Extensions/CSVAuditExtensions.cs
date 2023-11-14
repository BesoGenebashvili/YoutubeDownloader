using System.Globalization;
using YoutubeDownloader.Models;
using static YoutubeDownloader.Models.DownloadResult;

namespace YoutubeDownloader.Extensions;

public static class CSVAuditExtensions
{
    private const string TimestampFormat = "yyyy-MM-dd hh:mm:ss";

    private static FileFormat ParseFileFormat(string s) =>
        Enum.Parse<FileFormat>(s, true);

    private static DateTime ParseTimestamp(string s) =>
        DateTime.ParseExact(s, TimestampFormat, CultureInfo.InvariantCulture);

    public static Success ParseSuccess(string s) =>
        s.Split(',') is [{ } videoId, { } fileName, { } fileFormat, { } timestamp, { } fileSizeInMB]
         ? new(
             videoId,
             fileName,
             ParseFileFormat(fileFormat),
             ParseTimestamp(timestamp),
             double.Parse(fileSizeInMB))
         : throw new CsvDataCorruptedException($"Error while parsing {nameof(Success)} type");

    public static (Failure failure, uint retryCount) ParseFailure(string s)
    {
        switch (s.Split(','))
        {
            case [
            { } videoId,
            { } fileFormat,
            { } timestamp,
            { } retryCount,
            { } errorMessage]:

                var failure = new Failure(
                    videoId,
                    ParseFileFormat(fileFormat),
                    ParseTimestamp(timestamp),
                    errorMessage);

                return (failure, uint.Parse(retryCount));

            default:
                throw new CsvDataCorruptedException($"Error while parsing {nameof(Failure)} type");
        }
    }

    public static string ToCSVColumn(this Success self)
    {
        string[] columns =
            [
                self.VideoId,
                self.FileFormat
                    .ToString()
                    .ToLower(),
                self.Timestamp
                    .ToString(TimestampFormat),
                self.FileSizeInMB
                    .ToString("F2")
            ];

        return string.Join(',', columns);
    }

    public static string ToCSVColumn(this Failure self, uint retryCount = 0)
    {
        string[] columns =
            [
                self.VideoId,
                self.FileFormat
                    .ToString()
                    .ToLower(),
                self.Timestamp
                    .ToString(TimestampFormat),
                retryCount.ToString(CultureInfo.InvariantCulture),
                self.ErrorMessage
                    .Replace(',', '.')
            ];

        return string.Join(',', columns);
    }
}
