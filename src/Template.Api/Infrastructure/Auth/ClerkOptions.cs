using System.ComponentModel.DataAnnotations;

namespace Template.Api.Infrastructure.Auth;

/// <summary>Settings for validating Clerk session tokens. Bound from the <c>Clerk</c> section.</summary>
public sealed class ClerkOptions
{
    public const string SectionName = "Clerk";

    /// <summary>
    /// The instance's Frontend API URL, which is the token issuer (<c>iss</c>).
    /// Development: <c>https://&lt;slug&gt;.clerk.accounts.dev</c>. Production: <c>https://clerk.&lt;your-domain&gt;</c>.
    /// Signing keys are fetched from <c>{Issuer}/.well-known/openid-configuration</c> and refreshed automatically.
    /// </summary>
    [Required]
    [Url]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Frontend origins allowed to obtain tokens for this API, checked against the <c>azp</c> claim.
    /// Clerk strongly recommends this to prevent CSRF-style token reuse from other sites.
    /// </summary>
    [MinLength(1, ErrorMessage = "Set at least one Clerk:AuthorizedParties origin, e.g. https://app.example.com.")]
    public string[] AuthorizedParties { get; set; } = [];

    /// <summary>
    /// Reject sessions with <c>sts=pending</c> (signed up but not yet in a required organization).
    /// </summary>
    public bool RejectPendingSessions { get; set; } = true;
}
