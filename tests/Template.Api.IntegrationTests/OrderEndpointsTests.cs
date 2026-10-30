using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Template.Application.Orders;
using Template.Domain.Orders;

namespace Template.Api.IntegrationTests;

public sealed class OrderEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateThenGet_ReturnsCreatedOrder()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/orders", new { customerName = "Acme A/S", totalAmount = 99.95m }, Ct);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<OrderResponse>(ApiJson.Options, Ct);
        Assert.NotNull(created);
        Assert.Equal(OrderStatus.Pending, created.Status);
        Assert.Equal($"/api/v1/orders/{created.Id}", create.Headers.Location?.AbsolutePath);

        var fetched = await _client.GetFromJsonAsync<OrderResponse>($"/api/v1/orders/{created.Id}", ApiJson.Options, Ct);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Acme A/S", fetched.CustomerName);
        Assert.Equal(99.95m, fetched.TotalAmount);
    }

    [Fact]
    public async Task Create_InvalidBody_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/orders", new { customerName = "", totalAmount = 0 }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.NotNull(problem);
        Assert.Contains("CustomerName", problem.Errors.Keys);
        Assert.Contains("TotalAmount", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_AmountAsString_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/orders", new { customerName = "Acme", totalAmount = "10" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_OrderOfAnotherUser_Returns404()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/orders", new { customerName = "Alice Corp", totalAmount = 10m }, Ct);
        var created = await create.Content.ReadFromJsonAsync<OrderResponse>(ApiJson.Options, Ct);

        var bob = factory.CreateAuthenticatedClient("user_bob");
        var response = await bob.GetAsync($"/api/v1/orders/{created!.Id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404Problem()
    {
        var response = await _client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
