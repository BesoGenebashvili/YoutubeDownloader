using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace YoutubeDownloader.Validation;

public sealed class ValidateOptions<TOptions>(IEnumerable<IValidator<TOptions>> validators)
    : IValidateOptions<TOptions> where TOptions : class
{
    private readonly IEnumerable<IValidator<TOptions>> _validators = validators;

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var context = new ValidationContext<TOptions>(options);

        var failures = _validators.SelectMany(v => v.Validate(context).Errors)
                                  .Where(vr => vr is not null)
                                  .Select(vr => vr.ErrorMessage)
                                  .ToList();

        return failures.Count == 0
                       ? ValidateOptionsResult.Success
                       : ValidateOptionsResult.Fail(failures);
    }
}

public static class ValidateOptionsExtensions
{
    public static OptionsBuilder<TOptions> ValidateFluently<TOptions>(this OptionsBuilder<TOptions> optionsBuilder)
        where TOptions : class
    {
        optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>, ValidateOptions<TOptions>>();

        return optionsBuilder;
    }
}
