using System.IO.Compression;
using System.Text.RegularExpressions;

namespace YoutubeDownloader.Extensions;

public static class FileSystemExtensions
{
    public static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public static bool FileExistsWithFirstLine(string filePath, string firstLine) =>
        File.Exists(filePath) &&
        File.ReadAllLines(filePath)
            .FirstOrDefault() == firstLine;

    public static string? RemoveInvalidCharactersFromFileName(string fileName)
    {
        var regexPattern = $"[{Regex.Escape(new(Path.GetInvalidFileNameChars()))}]";

        return Regex.Replace(fileName, regexPattern, string.Empty);
    }

    public async static Task UnpackFromZipInFileAsync(
        this Stream self,
        string zipEntryName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        using var zip = new ZipArchive(self, ZipArchiveMode.Read);

        var zipEntry = zip.GetEntry(zipEntryName) ?? throw new FileNotFoundException($"{zipEntryName} not found in archive");

        await using var zipEntryStream = zipEntry.Open();
        await using var fileStream = File.Create(filePath);

        await zipEntryStream.CopyToAsync(fileStream, cancellationToken)
                            .ConfigureAwait(false);
    }
}
