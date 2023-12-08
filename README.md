# YouTubeDownloader

YouTubeDownloader is a user-friendly console application for effortlessly downloading content from various sources in different qualities and formats. It includes a CSV auditing system to track download results.

![console](https://github.com/BesoGenebashvili/YoutubeDownloader/assets/52665934/acc4f94a-bc23-4863-9f4d-213f97911b04)

## Table of Contents

1. [Installation](#installation)
2. [TODO: Getting Started](#todo-getting-started)
3. [Configuration](#configuration)
4. [Technologies Used](#technologies-used)
5. [License](#license)


## Installation

### Prerequisites

- **.NET 8.0:** Ensure you have .NET 8.0 installed on your system.

### Console Font Setup (Recommended for Better Experience)

To enhance readability in the console:

1. **Download:** Get the Cascadia Code font from [here](https://github.com/microsoft/cascadia-code).
2. **Install:** Double-click the downloaded font file and follow the installation prompts.
3. **Set Font in Console:**
   - Open Command Prompt or PowerShell.
   - Right-click the title bar, select "Properties."
   - Go to the "Font" tab, choose "Cascadia Code," click "OK."

Setting the console font to Cascadia Code will improve your experience while using app.

## Configuration

In the `appsettings.json` file, you can adjust the configuration settings.  
The program creates necessary files and folders and downloads [FFmpeg](https://ffmpeg.org) compatible with your system.

#### `downloaderSettings`

- `saveFolderPath`: Specifies the folder where downloaded files will be stored.
- `ffmpegPath`: Path to the FFmpeg executable.

#### `auditSettings.csvSettings`

- `auditSuccessful`: Enable/disable auditing for successful downloads.
- `auditFailed`: Enable/disable auditing for failed downloads.
- `successfulDownloadsFilePath`: File path for storing details of successful downloads.
- `failedDownloadsFilePath`: File path for storing details of failed downloads.

## Technologies Used

- **YoutubeExplode:** A .NET library for interacting with YouTube.
- **FluentValidation:** A popular .NET library for building strongly-typed validation rules.
- **Custom CSV Audit:** A CSV auditing system for logging successful and failed downloads.
- **AnsiConsole:** A library for enhancing console output with ANSI escape codes.
- **Terminalizer:** A tool for recording terminal sessions into animated GIFs or JSON files.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
