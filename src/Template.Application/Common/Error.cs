namespace Template.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>An expected failure. Unexpected failures are exceptions.</summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IDictionary<string, string[]>? ValidationErrors = null)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Validation(IDictionary<string, string[]> errors) =>
        new("validation_failed", "One or more validation errors occurred.", ErrorType.Validation, errors);
}
