using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Template.Application.Orders;
using Template.Domain.Orders;

namespace Template.Api.IntegrationTests;

/// <summary>PUT and DELETE semantics: idempotency, 204, 404 and 409.</summary>
public sealed class OrderUpdateDeleteTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient($"user_{Guid.NewGuid():N}");
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<OrderResponse> CreateAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/orders", new { customerName = "Acme", totalAmount = 10m }, Ct);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(ApiJson.Options, Ct))!;
    }

    private Task<HttpResponseMessage> PutAsync(Guid id, string name, decimal amount, string status) =>
        _client.PutAsJsonAsync($"/api/v1/orders/{id}", new { customerName = name, totalAmount = amount, status }, Ct);

    [Fact]
    public async Task Put_ValidUpdate_Returns200WithUpdatedOrder()
    {
        var order = await CreateAsync();

        var response = await PutAsync(order.Id, "Acme Nordic", 25m, "Confirmed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<OrderResponse>(ApiJson.Options, Ct);
        Assert.Equal("Acme Nordic", updated!.CustomerName);
        Assert.Equal(OrderStatus.Confirmed, updated.Status);
    }

    [Fact]
    public async Task Put_SameRequestTwice_IsIdempotent()
    {
        var order = await CreateAsync();

        var first = await PutAsync(order.Id, "Acme", 10m, "Cancelled");
        var second = await PutAsync(order.Id, "Acme", 10m, "Cancelled");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Put_ChangeDetailsOfConfirmedOrder_Returns409()
    {
        var order = await CreateAsync();
        await PutAsync(order.Id, "Acme", 10m, "Confirmed");

        var response = await PutAsync(order.Id, "Changed", 10m, "Confirmed");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal("order_not_modifiable", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Put_InvalidStatusTransition_Returns409()
    {
        var order = await CreateAsync();
        await PutAsync(order.Id, "Acme", 10m, "Cancelled");

        var response = await PutAsync(order.Id, "Acme", 10m, "Pending");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_InvalidBody_Returns400()
    {
        var order = await CreateAsync();

        var response = await PutAsync(order.Id, "", 0m, "Pending");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_OtherUsersOrder_Returns404()
    {
        var order = await CreateAsync();
        var other = factory.CreateAuthenticatedClient($"user_{Guid.NewGuid():N}");

        var response = await other.PutAsJsonAsync($"/api/v1/orders/{order.Id}", new { customerName = "x", totalAmount = 1m, status = "Pending" }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingOrder_Returns204ThenGetReturns404()
    {
        var order = await CreateAsync();

        var delete = await _client.DeleteAsync($"/api/v1/orders/{order.Id}", Ct);
        var get = await _client.GetAsync($"/api/v1/orders/{order.Id}", Ct);
        var deleteAgain = await _client.DeleteAsync($"/api/v1/orders/{order.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
    }

    [Fact]
    public async Task Delete_OtherUsersOrder_Returns404AndKeepsOrder()
    {
        var order = await CreateAsync();
        var other = factory.CreateAuthenticatedClient($"user_{Guid.NewGuid():N}");

        var response = await other.DeleteAsync($"/api/v1/orders/{order.Id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/orders/{order.Id}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Post_NonJsonContentType_Returns415()
    {
        var response = await _client.PostAsync(
            "/api/v1/orders",
            new StringContent("customerName=Acme", Encoding.UTF8, "application/x-www-form-urlencoded"),
            Ct);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Post_ToItemUri_Returns405()
    {
        var order = await CreateAsync();

        var response = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}", new { }, Ct);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
