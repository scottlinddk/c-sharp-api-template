using System.Text.Json.Serialization;
using Template.Api.Endpoints.Health;
using Template.Api.Endpoints.Orders;
using Template.Api.Infrastructure.ExceptionHandling;
using Template.Api.Infrastructure.Http;
using Template.Api.Infrastructure.OpenApi;
using Template.Application;
using Template.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Web defaults accept numbers sent as strings, which makes OpenAPI describe every number as
    // "number | string". Strict keeps the contract (and the generated TS client) clean.
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

builder.Services.AddApiOpenApi();
builder.Services.AddApiForwardedHeaders(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);

var app = builder.Build();

// TLS terminates at Caddy / the Hetzner LB, so no HTTPS redirection here.
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.MapApiOpenApi();
app.MapHealthEndpoints();

var v1 = app.MapGroup("/api/v1");
v1.MapOrderEndpoints();

await app.RunAsync();
