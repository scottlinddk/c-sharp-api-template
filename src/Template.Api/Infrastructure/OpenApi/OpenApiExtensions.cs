using Microsoft.OpenApi;

namespace Template.Api.Infrastructure.OpenApi;

internal static class OpenApiExtensions
{
    private const string DocumentName = "v1";
    private const string EnabledKey = "OpenApi:Enabled";

    /// <summary>
    /// Generates the OpenAPI document with the built-in Microsoft.AspNetCore.OpenApi package.
    /// XML doc comments and endpoint metadata (WithSummary, typed results) feed the document.
    /// </summary>
    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Template API",
                    Version = DocumentName,
                    Description = "C# API template hosted on Hetzner. Errors follow RFC 9457 (application/problem+json).",
                };
                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Serves /openapi/v1.json and Swagger UI at /swagger when <c>OpenApi:Enabled</c> is true.
    /// Enabled in Development by default; enable per environment with OpenApi__Enabled=true.
    /// </summary>
    public static WebApplication MapApiOpenApi(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(EnabledKey))
        {
            return app;
        }

        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/openapi/{DocumentName}.json", $"Template API {DocumentName}");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Template API";
            options.DisplayRequestDuration();
        });

        return app;
    }
}
