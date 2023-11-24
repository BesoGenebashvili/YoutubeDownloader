using System.Text;
using System.Runtime.InteropServices;

namespace YoutubeDownloader;

#pragma warning disable CA1401 // P/Invokes should not be visible
public static unsafe partial class ConsoleInterop
{
    public const int MF_BYCOMMAND = 0x00000000;
    public const int SC_MINIMIZE = 0xF020;
    public const int SC_MAXIMIZE = 0xF030;
    public const int SC_SIZE = 0xF000;

    [LibraryImport("user32.dll")]
    public static partial int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

    [LibraryImport("kernel32.dll")]
    public static partial IntPtr GetConsoleWindow();
}
#pragma warning restore CA1401 // P/Invokes should not be visible

public static class ConsoleExtensions
{
    public unsafe static void Configure()
    {
        try
        {
            DisableWindowButtons();
            AdjustWindowSize();
            SetEncoding();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while configuring the console settings: {ex.Message}");
            // TODO: Log.Error
        }

        unsafe static void DisableWindowButtons()
        {
            var consoleWindow = ConsoleInterop.GetSystemMenu(ConsoleInterop.GetConsoleWindow(), false);

#pragma warning disable CA1806 // Do not ignore method results
            ConsoleInterop.DeleteMenu(consoleWindow, ConsoleInterop.SC_MINIMIZE, ConsoleInterop.MF_BYCOMMAND);
            ConsoleInterop.DeleteMenu(consoleWindow, ConsoleInterop.SC_MAXIMIZE, ConsoleInterop.MF_BYCOMMAND);
            ConsoleInterop.DeleteMenu(consoleWindow, ConsoleInterop.SC_SIZE, ConsoleInterop.MF_BYCOMMAND);
#pragma warning restore CA1806 // Do not ignore method results
        }

        static void AdjustWindowSize()
        {
            Console.WindowHeight = Console.LargestWindowHeight / 2;
            Console.WindowWidth = Console.LargestWindowWidth / 2;
        }

        static void SetEncoding()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
    }
}