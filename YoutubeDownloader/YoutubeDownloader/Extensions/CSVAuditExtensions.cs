using System.Globalization;
using YoutubeDownloader.Models;
using static YoutubeDownloader.Models.DownloadResult;

namespace YoutubeDownloader.Extensions;

public static class CSVAuditExtensions
{
    private const string TimestampFormat = "yyyy-MM-dd hh:mm:ss";

    private static VideoConfiguration ParseVideoConfiguration(string fileFormat, string videoQuality) =>
        Enum.Parse<FileFormat>(fileFormat, true) switch
        {
            FileFormat.MP3 => new VideoConfiguration.MP3(Enum.Parse<AudioQuality>(videoQuality, true)),
            FileFormat.MP4 => new VideoConfiguration.MP4(Enum.Parse<VideoQuality>(videoQuality, true)),
            _ => throw new NotImplementedException(nameof(fileFormat))
        };

    private static DateTime ParseTimestamp(string s) =>
        DateTime.ParseExact(s, TimestampFormat, CultureInfo.InvariantCulture);

    private static (string fileFormat, string quality) ToCSVColumns(this VideoConfiguration self) =>
        self switch
        {
            VideoConfiguration.MP3(var q) => (FileFormat.MP3.ToString().ToLower(), q.ToString()),
            VideoConfiguration.MP4(var q) => (FileFormat.MP4.ToString().ToLower(), q.ToString()),
            _ => throw new NotImplementedException(nameof(self))
        };

    public static Success ParseSuccess(string s) =>
        s.Split(',') is [{ } videoId, { } fileName, { } fileFormat, { } videoQuality, { } timestamp, { } fileSizeInMB]
         ? new(
             videoId,
             fileName,
             ParseVideoConfiguration(fileFormat, videoQuality),
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
            { } videoQuality,
            { } timestamp,
            { } retryCount,
            { } errorMessage]:

                var failure = new Failure(
                    videoId,
                    ParseVideoConfiguration(fileFormat, videoQuality),
                    ParseTimestamp(timestamp),
                    errorMessage);

                return (failure, uint.Parse(retryCount));

            default:
                throw new CsvDataCorruptedException($"Error while parsing {nameof(Failure)} type");
        }
    }

    public static string ToCSVColumn(this Success self)
    {
        var (fileFormat, quality) = self.Configuration
                                        .ToCSVColumns();

        string[] columns =
            [
                self.VideoId,
                fileFormat,
                quality,
                self.Timestamp
                    .ToString(TimestampFormat),
                self.FileSizeInMB
                    .ToString("F2")
            ];

        return string.Join(',', columns);
    }

    public static string ToCSVColumn(this Failure self, uint retryCount = 0)
    {
        var (fileFormat, quality) = self.Configuration
                                        .ToCSVColumns();

        string[] columns =
            [
                self.VideoId,
                fileFormat,
                quality,
                self.Timestamp
                    .ToString(TimestampFormat),
                retryCount.ToString(CultureInfo.InvariantCulture),
                self.ErrorMessage
                    .Replace(',', '.')
            ];

        return string.Join(',', columns);
    }
}
