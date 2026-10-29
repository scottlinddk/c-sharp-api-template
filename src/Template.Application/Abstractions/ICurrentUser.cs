namespace Template.Application.Abstractions;

/// <summary>
/// The authenticated caller. Implemented in the Api layer from the validated Clerk session token,
/// so use cases never touch HTTP or JWT types.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Clerk user id (the <c>sub</c> claim), e.g. <c>user_123</c>.</summary>
    string UserId { get; }

    /// <summary>Active Clerk organization id (the <c>o.id</c> claim), if any.</summary>
    string? OrganizationId { get; }
}
