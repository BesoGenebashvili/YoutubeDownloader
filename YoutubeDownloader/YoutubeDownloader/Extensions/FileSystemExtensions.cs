using System.IO.Compression;

namespace YoutubeDownloader.Extensions;

public static class FileSystemExtensions
{
    public static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
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
