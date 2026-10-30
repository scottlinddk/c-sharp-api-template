namespace Template.Api.Infrastructure.Auth;

/// <summary>Claim names in Clerk session tokens (v2). See clerk.com/docs/guides/sessions/session-tokens.</summary>
internal static class ClerkClaimTypes
{
    public const string Subject = "sub";
    public const string SessionId = "sid";
    public const string AuthorizedParty = "azp";
    public const string SessionStatus = "sts";

    /// <summary>Organization claim: a JSON object with <c>id</c>, <c>slg</c>, <c>rol</c>, <c>per</c>, <c>fpm</c>.</summary>
    public const string Organization = "o";
}
