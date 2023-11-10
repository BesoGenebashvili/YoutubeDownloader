using YoutubeDownloader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;
using FluentValidation;

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
            services.AddValidatorsFromAssemblyContaining<Program>();

            services.AddOptions<DownloaderSettings>()
                    .Bind(host.Configuration.GetSection(DownloaderSettings.SectionName))
                    .ValidateFluently()
                    .ValidateOnStart();

            services.Configure<CSVSettings>(host.Configuration.GetSection(nameof(CSVSettings)));

            services.AddSingleton<YoutubeClient>();

            services.AddTransient<YoutubeService>();
            services.AddTransient<IAuditService, CSVAuditService>();

            services.AddSingleton<App>();
        });