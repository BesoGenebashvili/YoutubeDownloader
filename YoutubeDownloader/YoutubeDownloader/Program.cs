using YoutubeDownloader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;

Console.WriteLine("Hello, World!");

using var host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

// TODO: Error Handler Middleware ?
try
{
    ConsoleExtensions.Configure();
    FFmpegExtensions.Configure();

    await services.GetRequiredService<App>()
                  .RunAsync(args)
                  .ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((host, services) =>
        {
            services.AddSingleton<YoutubeClient>();

            services.Configure<DownloaderSettings>(host.Configuration.GetSection(nameof(DownloaderSettings)));
            services.Configure<CSVSettings>(host.Configuration.GetSection(nameof(CSVSettings)));

            services.AddTransient<YoutubeService>();
            services.AddTransient<IAuditService, CSVAuditService>();

            services.AddSingleton<App>();
        });
