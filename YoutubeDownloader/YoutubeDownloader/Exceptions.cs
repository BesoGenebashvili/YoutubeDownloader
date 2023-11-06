namespace YoutubeDownloader;

public sealed class CsvDataException : Exception
{
    public CsvDataException() { }
    public CsvDataException(string message) : base(message) { }
    public CsvDataException(string message, Exception innerException) : base(message, innerException) { }
}
