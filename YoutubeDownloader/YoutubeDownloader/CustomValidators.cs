using FluentValidation;
using FluentValidation.Validators;

namespace YoutubeDownloader;

public class FolderPathValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "FolderPathValidator";

    public override bool IsValid(ValidationContext<T> context, string value) =>
        Directory.Exists(value);

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "{PropertyName} must be a valid folder path.";
}

public class FilePathValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "FilePathValidator";

    public override bool IsValid(ValidationContext<T> context, string value) =>
        File.Exists(value);

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "{PropertyName} must be a valid file path.";
}

public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidFolderPath<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.SetValidator(new FolderPathValidator<T>());

    public static IRuleBuilderOptions<T, string> MustBeValidFilePath<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.SetValidator(new FilePathValidator<T>());
}