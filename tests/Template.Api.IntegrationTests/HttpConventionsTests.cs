using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Template.Api.IntegrationTests;

/// <summary>Correlation id and CORS behavior.</summary>
public sealed class HttpConventionsTests(ApiFactory factory)
{
    private const string Header = "Correlation-ID";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CorrelationId_FromClient_IsEchoed()
    {
        var client = factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add(Header, "aaaa0000-bb11-2222-33cc-444444dddddd");

        var response = await client.GetAsync("/api/v1/orders", Ct);

        Assert.Equal("aaaa0000-bb11-2222-33cc-444444dddddd", response.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task CorrelationId_Missing_IsGenerated()
    {
        var response = await factory.CreateClient().GetAsync("/health/live", Ct);

        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues(Header).Single()));
    }

    [Fact]
    public async Task CorrelationId_Unsafe_IsReplaced()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation(Header, "bad value\twith spaces");

        var response = await client.GetAsync("/health/live", Ct);

        Assert.NotEqual("bad value\twith spaces", response.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task CorrelationId_IsIncludedInProblemDetails()
    {
        var client = factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add(Header, "corr-404-test");

        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}", Ct);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));

        Assert.Equal("corr-404-test", body.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Cors_ExposesLocationAndCorrelationId()
    {
        var client = factory.CreateAuthenticatedClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(new { customerName = "Acme", totalAmount = 1m }),
        };
        request.Headers.Add("Origin", TestTokens.AllowedOrigin);

        var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var exposed = response.Headers.GetValues("Access-Control-Expose-Headers").Single();
        Assert.Contains("Location", exposed);
        Assert.Contains(Header, exposed);
    }
}
