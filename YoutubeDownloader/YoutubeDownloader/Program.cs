﻿using YoutubeDownloader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;
using FluentValidation;
using YoutubeDownloader.Settings;
using YoutubeDownloader.Validation;
using YoutubeDownloader.Services.Implementations;
using YoutubeDownloader.Services.Abstractions;
using YoutubeDownloader.Extensions;

Console.WriteLine("Hello, World!");

using var host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

try
{
    ConsoleExtensions.Configure();

    // TODO: Estimate download time with GetInternetSpeed method
    using var ffmpegTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    await FFmpegExtensions.ConfigureAsync(ffmpegTokenSource.Token);

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
                    .BindConfiguration(DownloaderSettings.SectionName)
                    .ValidateFluently()
                    .ValidateOnStart();

            services.AddOptions<CSVSettings>()
                    .BindConfiguration(CSVSettings.SectionName)
                    .ValidateFluently()
                    .ValidateOnStart();

            services.AddSingleton<YoutubeClient>();
            services.AddTransient<YoutubeService>();
            services.AddTransient<IAuditService, CSVAuditService>();

            services.AddSingleton<App>();
        });