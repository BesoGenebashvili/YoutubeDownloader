using Microsoft.Extensions.Options;
using System.Globalization;
using YoutubeExplode.Videos.Streams;
using static YoutubeDownloader.DownloadResult;

namespace YoutubeDownloader;

public sealed record CSVSettings(
    bool AuditSuccessful,
    bool AuditFailed,
    string SuccessfulDownloadsFilePath,
    string FailedDownloadsFilePath);

public static class CSVAuditExtensions
{
    private const string CSVTimestampFormat = "yyyy-MM-dd hh:mm:ss";

    private static FileFormat ParseFileFormat(string s) =>
        Enum.Parse<FileFormat>(s, true);

    private static DateTime ParseTimestamp(string s) =>
        DateTime.ParseExact(s, CSVTimestampFormat, CultureInfo.InvariantCulture);

    public static Success ParseSuccess(string s) =>
        s.Split(',') is [{ } videoId, { } fileName, { } fileFormat, { } timestamp, { } fileSizeInMB]
         ? new(
             videoId,
             fileName,
             ParseFileFormat(fileFormat),
             ParseTimestamp(timestamp),
             double.Parse(fileSizeInMB))
         : throw new CsvDataException($"Error while parsing {nameof(Success)} type");

    public static (Failure failure, uint retryCount) ParseFailure(string s)
    {
        switch (s.Split(','))
        {
            case [
            { } videoId,
            { } fileFormat,
            { } timestamp,
            { } errorMessage,
            { } retryCount]:

                var failure = new Failure(
                    videoId,
                    ParseFileFormat(fileFormat),
                    ParseTimestamp(timestamp),
                    errorMessage);

                return (failure, uint.Parse(retryCount));

            default:
                throw new CsvDataException($"Error while parsing {nameof(Failure)} type");
        }
    }

    public static string ToCSVColumn(this Success self) =>
        $"{self.VideoId},{self.FileFormat.ToString().ToLower()},{self.Timestamp.ToString(CSVTimestampFormat)},{self.FileSizeInMB:F2}";

    public static string ToCSVColumn(this Failure self, uint retryCount = 0) =>
        $"{self.VideoId},{self.FileFormat.ToString().ToLower()},{self.Timestamp.ToString(CSVTimestampFormat)},{retryCount},{self.ErrorMessage},";
}

public sealed class CSVAuditService(IOptions<CSVSettings> options) : IAuditService
{
    private readonly CSVSettings _settings = options.Value;

    public async Task AuditDownloadsAsync(
        IReadOnlyCollection<DownloadResult> downloads,
        CancellationToken cancellationToken = default)
    {
        var successes = downloads.OfType<Success>()
                                 .ToList()
                                 .AsReadOnly();

        var failures = downloads.OfType<Failure>()
                                .ToList()
                                .AsReadOnly();

        Task[] auditTasks = (_settings.AuditSuccessful, _settings.AuditFailed) switch
        {
            (true, true) => [AuditSuccessfulDownloadsAsync(successes, cancellationToken), AuditFailedDownloadsAsync(failures, cancellationToken)],
            (true, _) => [AuditSuccessfulDownloadsAsync(successes, cancellationToken)],
            (_, true) => [AuditFailedDownloadsAsync(failures, cancellationToken)],
            _ => []
        };

        await Task.WhenAll(auditTasks)
                  .ConfigureAwait(false);

        // Log
    }

    private async Task AuditSuccessfulDownloadsAsync(
        IReadOnlyCollection<Success> successes,
        CancellationToken cancellationToken = default)
    {
        // TODO: add "File Absolute Path"
        var headers = string.Join(',', ["Video Id", "File Name", "File Format", "Timestamp", "File Size In MB"]);

        var records = successes.Select(CSVAuditExtensions.ToCSVColumn);

        var content = File.Exists(_settings.SuccessfulDownloadsFilePath)
                          ? records
                          : records.Prepend(headers);

        await File.AppendAllLinesAsync(
                      _settings.SuccessfulDownloadsFilePath,
                      content,
                      cancellationToken)
                  .ConfigureAwait(false);
    }

    public async Task AuditFailedDownloadsAsync(
        IReadOnlyCollection<Failure> failures,
        CancellationToken cancellationToken = default)
    {
        var headers = string.Join(',', ["Video Id", "File Format", "Timestamp", "Retry Count", "Error Message"]);

        throw new NotImplementedException();
    }
}
