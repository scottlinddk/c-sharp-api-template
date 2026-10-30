using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Template.Api.Infrastructure.OpenApi;

internal static class OpenApiExtensions
{
    private const string DocumentName = "v1";
    private const string EnabledKey = "OpenApi:Enabled";
    private const string BearerSchemeName = "Bearer";

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

            // Lets Swagger UI send "Authorization: Bearer <Clerk session token>" via the Authorize button.
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Clerk session token. In the frontend: `await window.Clerk.session.getToken()`.",
                };
                return Task.CompletedTask;
            });

            // Marks every operation that is not AllowAnonymous as requiring the bearer token, and documents 401.
            options.AddOperationTransformer((operation, context, _) =>
            {
                var allowsAnonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
                if (allowsAnonymous)
                {
                    return Task.CompletedTask;
                }

                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerSchemeName, context.Document)] = [],
                });

                operation.Responses ??= [];
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Missing or invalid Clerk session token." });
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

        app.MapOpenApi().AllowAnonymous();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/openapi/{DocumentName}.json", $"Template API {DocumentName}");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Template API";
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization();
        });

        return app;
    }
}
