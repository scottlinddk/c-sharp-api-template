using System.Net;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Template.Api.IntegrationTests;

/// <summary>Clerk token validation rules, per clerk.com/docs/guides/sessions/manual-jwt-verification.</summary>
public sealed class AuthenticationTests(ApiFactory factory)
{
    private const string ProtectedPath = "/api/v1/orders/00000000-0000-0000-0000-000000000001";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NoToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync(ProtectedPath, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_PassesAuthentication()
    {
        var response = await factory.CreateClient().WithToken(TestTokens.Create()).GetAsync(ProtectedPath, Ct);

        // Authenticated; the order simply does not exist.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithoutAzp_IsAccepted()
    {
        // Clerk omits azp when the browser sent no Origin header; its guide says to skip the check then.
        var response = await factory.CreateClient().WithToken(TestTokens.Create(azp: null)).GetAsync(ProtectedPath, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public static TheoryData<string, string> RejectedTokens => new()
    {
        { "unknown azp", TestTokens.Create(azp: "https://evil.example") },
        { "pending session", TestTokens.Create(sessionStatus: "pending") },
        { "wrong issuer", TestTokens.Create(issuer: "https://other.clerk.accounts.dev") },
        { "expired", TestTokens.Create(issuedAt: DateTimeOffset.UtcNow.AddMinutes(-2), lifetime: TimeSpan.FromSeconds(60)) },
        { "untrusted key", TestTokens.Create(signingKey: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" }) },
    };

    [Theory]
    [MemberData(nameof(RejectedTokens))]
    public async Task InvalidToken_Returns401(string reason, string token)
    {
        var response = await factory.CreateClient().WithToken(token).GetAsync(ProtectedPath, Ct);

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401 for {reason}, got {(int)response.StatusCode}.");
    }
}
