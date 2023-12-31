﻿using FluentValidation;
using YoutubeDownloader.Validation;

namespace YoutubeDownloader.Settings;

public sealed record CSVSettings
{
    public const string SectionName = "auditSettings:csvSettings";

    public bool AuditSuccessful { get; init; }
    public bool AuditFailed { get; init; }
    public string? SuccessfulDownloadsFilePath { get; init; }
    public string? FailedDownloadsFilePath { get; init; }
}

public sealed class CSVSettingsValidator : AbstractValidator<CSVSettings>
{
    public CSVSettingsValidator()
    {
        // TODO: use camel case naming convention in error messages

        When(s => s.AuditSuccessful,
            () =>
            {
                RuleFor(s => s.SuccessfulDownloadsFilePath)
                    .NotEmpty()
                    .WithMessage(s => "{PropertyName} must not be empty when 'auditSuccessful' is true.");

                RuleFor(s => s.SuccessfulDownloadsFilePath)!
                    .MustBeValidFileName();
            });


        When(s => s.AuditFailed,
            () =>
            {
                RuleFor(s => s.FailedDownloadsFilePath)
                    .NotEmpty()
                    .WithMessage(s => "{PropertyName} must not be empty when 'auditFailed' is true.");

                RuleFor(s => s.FailedDownloadsFilePath)!
                    .MustBeValidFileName();
            });
    }
}