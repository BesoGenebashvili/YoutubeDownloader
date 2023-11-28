using System.Globalization;
using System.Text.RegularExpressions;
using YoutubeDownloader.Models;
using static YoutubeDownloader.Models.DownloadResult;

namespace YoutubeDownloader.Extensions;

public static class CSVAuditExtensions
{
    private const string TimestampFormat = "yyyy-MM-dd hh:mm:ss";

    public static readonly string[] SuccessHeaders =
        [
            "Video Id",
            "File Name",
            "File Format",
            "Quality",
            "Timestamp",
            "File Size In MB"
        ];

    public static readonly string[] FailureHeaders =
        [
            "Video Id",
            "File Format",
            "Quality",
            "Retry Count",
            "Timestamp",
            "Error Message"
        ];

    private static string ReplaceInvalidCharactersWithSemicolons(this string self) =>
        Regex.Replace(self, "[,\n]", ";");

    private static VideoConfiguration ParseVideoConfiguration(string fileFormat, string videoQuality) =>
        Enum.Parse<FileFormat>(fileFormat, true) switch
        {
            FileFormat.MP3 => new VideoConfiguration.MP3(Enum.Parse<AudioQuality>(videoQuality, true)),
            FileFormat.MP4 => new VideoConfiguration.MP4(Enum.Parse<VideoQuality>(videoQuality, true)),
            _ => throw new NotImplementedException(nameof(fileFormat))
        };

    private static DateTime ParseTimestamp(string value) =>
        DateTime.ParseExact(value, TimestampFormat, CultureInfo.InvariantCulture);

    private static (string fileFormat, string quality) ToCSVColumnValues(this VideoConfiguration self) =>
        self switch
        {
            VideoConfiguration.MP3(var q) => (FileFormat.MP3.ToString().ToLower(), q.ToString()),
            VideoConfiguration.MP4(var q) => (FileFormat.MP4.ToString().ToLower(), q.ToString()),
            _ => throw new NotImplementedException(nameof(self))
        };

    public static Success ParseSuccess(string value) =>
        value.Split(',') is [{ } videoId, { } fileName, { } fileFormat, { } videoQuality, { } timestamp, { } fileSizeInMB]
         ? new(
             videoId,
             fileName,
             ParseVideoConfiguration(fileFormat, videoQuality),
             ParseTimestamp(timestamp),
             double.Parse(fileSizeInMB))
         : throw new CsvDataCorruptedException($"Error while parsing {nameof(Success)} type");

    public static (Failure failure, uint retryCount) ParseFailure(string value)
    {
        switch (value.Split(','))
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
                                        .ToCSVColumnValues();

        string[] columns =
            [
                self.VideoId,
                self.FileName
                    .ReplaceInvalidCharactersWithSemicolons(),
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
                                        .ToCSVColumnValues();

        string[] columns =
            [
                self.VideoId,
                fileFormat,
                quality,
                self.Timestamp
                    .ToString(TimestampFormat),
                retryCount.ToString(CultureInfo.InvariantCulture),
                self.ErrorMessage
                    .ReplaceInvalidCharactersWithSemicolons()
            ];

        return string.Join(',', columns);
    }
}
