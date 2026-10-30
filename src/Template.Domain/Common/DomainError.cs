namespace Template.Domain.Common;

/// <summary>
/// A business rule that blocked an operation (for example an invalid state transition).
/// Returned rather than thrown, so callers can map it to an expected outcome such as 409 Conflict.
/// </summary>
public sealed record DomainError(string Code, string Message);
