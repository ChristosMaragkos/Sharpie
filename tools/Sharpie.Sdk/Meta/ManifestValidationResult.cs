namespace Sharpie.Sdk.Meta;

public class ManifestValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    private ManifestValidationResult(bool isValid, IReadOnlyList<string>? errors = null)
    {
        IsValid = isValid;
        Errors = errors ?? Array.Empty<string>();
    }

    public static ManifestValidationResult Valid { get; } = new ManifestValidationResult(true);

    public static ManifestValidationResult Invalid(IEnumerable<string> errors) =>
        new ManifestValidationResult(false, errors.ToList());
}
