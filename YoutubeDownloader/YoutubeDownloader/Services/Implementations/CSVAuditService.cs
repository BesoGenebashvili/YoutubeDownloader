using Microsoft.Extensions.Options;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Models;
using YoutubeDownloader.Services.Abstractions;
using YoutubeDownloader.Settings;
using static YoutubeDownloader.Models.DownloadResult;

namespace YoutubeDownloader.Services.Implementations;

public sealed class CSVAuditService(IOptions<CSVSettings> options) : IAuditService
{
    private readonly CSVSettings _settings = options.Value;

    public async Task AuditDownloadsAsync(
        IReadOnlyCollection<DownloadResult> downloads,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(downloads, nameof(downloads));

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

    public Task<IReadOnlyCollection<Success>> ListSuccessfulDownloads(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyCollection<Failure>> ListFailedDownloadsAsync(CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.FailedDownloadsFilePath, nameof(_settings.FailedDownloadsFilePath));

        var lines = await File.ReadAllLinesAsync(_settings.FailedDownloadsFilePath, cancellationToken)
                              .ConfigureAwait(false);

        return lines.Skip(1)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => CSVAuditExtensions.ParseFailure(l).failure)
                    .ToList()
                    .AsReadOnly();
    }

    private async Task AuditSuccessfulDownloadsAsync(
        IReadOnlyCollection<Success> successes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.SuccessfulDownloadsFilePath, nameof(_settings.SuccessfulDownloadsFilePath));

        // TODO: add "File Absolute Path"
        var headers = string.Join(',', ["Video Id", "File Name", "File Format", "Timestamp", "File Size In MB"]);

        var records = successes.Select(CSVAuditExtensions.ToCSVColumn);

        var content = ExistsWithFirstLine(_settings.SuccessfulDownloadsFilePath, headers)
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
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.FailedDownloadsFilePath, nameof(_settings.FailedDownloadsFilePath));

        var headers = string.Join(',', ["Video Id", "File Format", "Timestamp", "Retry Count", "Error Message"]);

        var availableFailedRecords = GetAvailableFailedRecords();

        // TODO: Refactor
        var records = failures.GroupBy(f => (f.VideoId, f.FileFormat))
                              .Select(g => (failure: g.MaxBy(f => f.Timestamp)!, retryCount: (uint)g.Count()))
                              .Concat(availableFailedRecords)
                              .GroupBy(f => (f.failure.VideoId, f.failure.FileFormat))
                              .Select(g => (g.MaxBy(f => f.failure.Timestamp).failure, retryCount: (uint)g.Count()));

        var content = records.Select(g => g.failure.ToCSVColumn(g.retryCount))
                             .Prepend(headers);

        await File.WriteAllLinesAsync(
                      _settings.FailedDownloadsFilePath,
                      content,
                      cancellationToken)
                  .ConfigureAwait(false);

        IEnumerable<(Failure failure, uint retryCount)> GetAvailableFailedRecords() =>
            ExistsWithFirstLine(_settings.FailedDownloadsFilePath, headers)
                ? File.ReadAllLines(_settings.FailedDownloadsFilePath)
                      .Skip(1)
                      .Select(CSVAuditExtensions.ParseFailure)
                : Enumerable.Empty<(Failure failure, uint retryCount)>();
    }

    private static bool ExistsWithFirstLine(string filePath, string firstLine) =>
        File.Exists(filePath) &&
        File.ReadAllLines(filePath)
            .FirstOrDefault() == firstLine;
}