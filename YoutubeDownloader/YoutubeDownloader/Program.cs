using YoutubeDownloader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;
using FluentValidation;
using YoutubeDownloader.Settings;
using YoutubeDownloader.Validation;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Services;
using Microsoft.Extensions.Options;
using Serilog;

using var host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

try
{
    ConsoleExtensions.Configure();

    var downloaderSettings = services.GetRequiredService<IOptions<DownloaderSettings>>()
                                     .Value;

    FileSystemExtensions.CreateDirectoryIfNotExists(downloaderSettings.SaveFolderPath);

    // TODO: Estimate download time with GetInternetSpeed method
    using var ffmpegConfigurationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    await FFmpegExtensions.ConfigureAsync(downloaderSettings.FFmpegPath, ffmpegConfigurationTokenSource.Token)
                          .ConfigureAwait(false);

    await services.GetRequiredService<App>()
                  .RunAsync(args)
                  .ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.WriteLine($"For more detailed information, please check the log file located at: .\\logs");
    Log.Error("An error occurred: {ErrorMessage}", ex);
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
        {
            const string logOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Indent:l}{Message}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                                .MinimumLevel.Information()
                                .WriteTo.File(
                                    path: "logs/log.txt",
                                    outputTemplate: logOutputTemplate,
                                    rollingInterval: RollingInterval.Day)
                                .CreateLogger();

            services.AddValidatorsFromAssemblyContaining<Program>();

            services.AddOptions<DownloaderSettings>()
                    .BindConfiguration(DownloaderSettings.SectionName)
                    .ValidateFluently()
                    .ValidateOnStart();

            services.AddOptions<CSVSettings>()
                    .BindConfiguration(CSVSettings.SectionName)
                    .ValidateFluently()
                    .ValidateOnStart();

            services.AddSingleton<YoutubeClient>();
            services.AddTransient<YoutubeDownloaderService>();
            services.AddTransient<YoutubeService>();
            services.AddTransient<IAuditService, CSVAuditService>();

            services.AddSingleton<App>();
        });