﻿using FluentValidation;
using YoutubeDownloader.Validation;

namespace YoutubeDownloader.Settings;

public sealed class DownloaderSettings
{
    public const string SectionName = "downloaderSettings";

    public required string SaveFolderPath { get; init; }
    public string? FFmpegPath { get; init; }
    public string? FileNameTemplate { get; init; }
}

public sealed class DownloaderSettingsValidator : AbstractValidator<DownloaderSettings>
{
    public DownloaderSettingsValidator()
    {
        // TODO: use camel case naming convention in error messages

        RuleFor(s => s.SaveFolderPath)
            .NotEmpty()
            .MustBeValidFolderName();

#pragma warning disable CS8620
        // dotnet/runetime issue #36510
        When(s => s.FFmpegPath != string.Empty,
            () =>
            {
                RuleFor(s => s.FFmpegPath)
                    .NotEmpty();

                RuleFor(s => s.FFmpegPath)
                    .MustBeValidFilePath();
            });
#pragma warning restore
    }
}