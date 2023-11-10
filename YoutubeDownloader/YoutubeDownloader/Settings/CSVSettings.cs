namespace YoutubeDownloader.Settings;

public sealed record CSVSettings
{
    public bool AuditSuccessful { get; init; }
    public bool AuditFailed { get; init; }
    public string? SuccessfulDownloadsFilePath { get; init; }
    public string? FailedDownloadsFilePath { get; init; }
}
