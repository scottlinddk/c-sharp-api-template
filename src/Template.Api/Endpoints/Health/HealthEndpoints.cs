using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Template.Api.Endpoints.Health;

internal static class HealthEndpoints
{
    /// <summary>
    /// /health/live: the process is up (no dependencies checked).
    /// /health/ready: dependencies tagged "ready" (the database) are reachable.
    /// Used by Caddy, the Hetzner LB and the deploy script. Excluded from the OpenAPI document.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .ExcludeFromDescription();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
            .ExcludeFromDescription();

        return app;
    }
}
