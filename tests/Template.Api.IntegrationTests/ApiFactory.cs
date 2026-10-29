using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Template.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Template.Api.IntegrationTests.ApiFactory))]

namespace Template.Api.IntegrationTests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL container (requires Docker).
/// Shared by all tests in the assembly; migrations run once at start-up.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("OpenApi:Enabled", "true");
        builder.UseSetting("Clerk:Issuer", TestTokens.Issuer);
        builder.UseSetting("Clerk:AuthorizedParties:0", TestTokens.AllowedOrigin);

        // Trust the test signing key instead of downloading Clerk's JWKS. Everything else
        // (issuer, lifetime, algorithm, azp and sts checks) runs exactly as in production.
        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
                configuration.SigningKeys.Add(TestTokens.SigningKey);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }));
    }

    /// <summary>A client that sends a valid token for <paramref name="userId"/>.</summary>
    public HttpClient CreateAuthenticatedClient(string userId = "user_alice") =>
        CreateClient().WithToken(TestTokens.Create(userId));

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
