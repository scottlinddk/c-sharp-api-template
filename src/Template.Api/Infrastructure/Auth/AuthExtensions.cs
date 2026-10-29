using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Template.Application.Abstractions;

namespace Template.Api.Infrastructure.Auth;

internal static class AuthExtensions
{
    /// <summary>
    /// Validates Clerk session tokens sent as <c>Authorization: Bearer &lt;token&gt;</c>, following
    /// Clerk's manual verification guide: RS256 signature, issuer, exp/nbf, and the azp allow-list.
    /// Every endpoint requires an authenticated user unless it opts out with AllowAnonymous().
    /// </summary>
    public static IServiceCollection AddClerkAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClerkOptions>()
            .Bind(configuration.GetSection(ClerkOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ClerkOptions>>((options, clerkOptions) =>
            {
                var clerk = clerkOptions.Value;

                options.Authority = clerk.Issuer;
                options.MapInboundClaims = false; // keep Clerk's claim names (sub, sid, azp, o)

                options.TokenValidationParameters.ValidIssuer = clerk.Issuer;
                options.TokenValidationParameters.ValidAlgorithms = ["RS256"];
                // Clerk session tokens carry no "aud" claim by default; azp is checked below instead.
                options.TokenValidationParameters.ValidateAudience = false;
                // Session tokens live ~60 s, so the default 5-minute skew would multiply their lifetime.
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(5);
                options.TokenValidationParameters.NameClaimType = ClerkClaimTypes.Subject;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var principal = context.Principal!;

                        // azp may be omitted by Clerk when the browser sent no Origin; Clerk's guide allows that.
                        var azp = principal.FindFirst(ClerkClaimTypes.AuthorizedParty)?.Value;
                        if (azp is not null && !clerk.AuthorizedParties.Contains(azp, StringComparer.OrdinalIgnoreCase))
                        {
                            context.Fail("Token azp is not an authorized party.");
                            return Task.CompletedTask;
                        }

                        if (clerk.RejectPendingSessions
                            && principal.FindFirst(ClerkClaimTypes.SessionStatus)?.Value == "pending")
                        {
                            context.Fail("Session is pending.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }

    /// <summary>Reads the <c>id</c> from Clerk's compact organization claim (<c>o</c>).</summary>
    internal static string? ReadOrganizationId(string? organizationClaimJson)
    {
        if (string.IsNullOrEmpty(organizationClaimJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(organizationClaimJson);
            return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
