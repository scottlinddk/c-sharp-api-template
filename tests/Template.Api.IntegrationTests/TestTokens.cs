using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Template.Api.IntegrationTests;

/// <summary>
/// Issues tokens shaped like Clerk session tokens (v2), signed with a per-run RSA key that the
/// test host trusts instead of Clerk's JWKS. No network access to Clerk is needed.
/// </summary>
public static class TestTokens
{
    public const string Issuer = "https://test-instance.clerk.accounts.dev";
    public const string AllowedOrigin = "https://app.test";

    public static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "test-key" };

    private static readonly JsonWebTokenHandler Handler = new();

    public static string Create(
        string userId = "user_alice",
        string? azp = AllowedOrigin,
        string issuer = Issuer,
        string? sessionStatus = null,
        TimeSpan? lifetime = null,
        DateTimeOffset? issuedAt = null,
        SecurityKey? signingKey = null)
    {
        var now = issuedAt ?? DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["sub"] = userId,
            ["sid"] = "sess_test",
            ["v"] = 2,
        };
        if (azp is not null)
        {
            claims["azp"] = azp;
        }
        if (sessionStatus is not null)
        {
            claims["sts"] = sessionStatus;
        }

        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(lifetime ?? TimeSpan.FromSeconds(60)).UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey ?? SigningKey, SecurityAlgorithms.RsaSha256),
        });
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }
}
