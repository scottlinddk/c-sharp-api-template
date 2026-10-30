using Microsoft.AspNetCore.HttpOverrides;

namespace Template.Api.Infrastructure.Http;

internal static class HttpExtensions
{
    /// <summary>
    /// Trusts X-Forwarded-For/Proto only from the configured proxy networks
    /// (the Docker network Caddy runs on, or the Hetzner private network in Phase 2).
    /// </summary>
    public static IServiceCollection AddApiForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }

    /// <summary>Explicit allow-list of frontend origins from <c>Cors:AllowedOrigins</c>.</summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Without this, browser JavaScript cannot read these response headers
            // (e.g. the Location of a newly created resource).
            .WithExposedHeaders("Location", CorrelationIdMiddleware.HeaderName)));

        return services;
    }
}
