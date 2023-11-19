namespace YoutubeDownloader.Extensions;

public enum OperatingSystem : byte
{
    Windows,
    Linux,
    MacOS
}

public static class OperatingSystemExtensions
{
    public static OperatingSystem GetOperatingSystem()
    {
        if (System.OperatingSystem.IsWindows())
            return OperatingSystem.Windows;
        else if (System.OperatingSystem.IsLinux())
            return OperatingSystem.Linux;
        else if (System.OperatingSystem.IsMacOS())
            return OperatingSystem.MacOS;

        throw new ApplicationException("OS not supported");
    }
}
