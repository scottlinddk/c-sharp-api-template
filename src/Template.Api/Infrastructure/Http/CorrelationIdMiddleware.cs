using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Template.Api.Infrastructure.Http;

/// <summary>
/// Propagates a correlation id per request (Microsoft REST API design guidance, "distributed tracing").
/// Reuses the client's <c>Correlation-ID</c> header when it is safe to log, otherwise uses the
/// W3C trace id that ASP.NET Core already created. The id is echoed in the response header,
/// added to the logging scope and the current Activity, and included in ProblemDetails.
/// </summary>
internal sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = SafeId().IsMatch(incoming)
            ? incoming
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            await next(context);
        }
    }

    public static string? Get(HttpContext context) => context.Items[ItemKey] as string;

    // Letters, digits and - _ . : only, max 128 chars. Anything else is replaced, to prevent log injection.
    [GeneratedRegex(@"^[A-Za-z0-9\-_.:]{1,128}$")]
    private static partial Regex SafeId();
}
