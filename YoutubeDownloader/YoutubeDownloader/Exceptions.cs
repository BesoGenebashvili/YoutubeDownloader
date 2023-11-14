namespace YoutubeDownloader;

public sealed class CsvDataCorruptedException : Exception
{
    public CsvDataCorruptedException() { }
    public CsvDataCorruptedException(string message) : base(message) { }
    public CsvDataCorruptedException(string message, Exception innerException) : base(message, innerException) { }
}
