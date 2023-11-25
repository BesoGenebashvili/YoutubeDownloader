using FluentValidation;
using FluentValidation.Validators;

namespace YoutubeDownloader.Validation;

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

public class FolderNameValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "FolderNameValidator";

    public override bool IsValid(ValidationContext<T> context, string value)
    {
        var invalidChars = Path.GetInvalidPathChars();

        return !value.Any(invalidChars.Contains);
    }

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "{PropertyName} must be a valid folder name.";
}

public class FileNameValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "FileNameValidator";

    public override bool IsValid(ValidationContext<T> context, string value)
    {
        var fileName = Path.GetFileName(value);

        var invalidChars = Path.GetInvalidFileNameChars();

        return !fileName.Any(invalidChars.Contains);
    }

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "{PropertyName} must be a valid file name.";
}

public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidFolderPath<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.SetValidator(new FolderPathValidator<T>());

    public static IRuleBuilderOptions<T, string> MustBeValidFilePath<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.SetValidator(new FilePathValidator<T>());

    public static IRuleBuilderOptions<T, string> MustBeValidFolderName<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.SetValidator(new FolderNameValidator<T>());

    public static IRuleBuilderOptions<T, string> MustBeValidFileName<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.SetValidator(new FileNameValidator<T>());
}