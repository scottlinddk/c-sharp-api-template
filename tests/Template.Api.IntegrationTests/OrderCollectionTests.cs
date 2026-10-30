using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Template.Application.Common;
using Template.Application.Orders;
using Template.Domain.Orders;

namespace Template.Api.IntegrationTests;

/// <summary>GET /orders: pagination, filtering, sorting and per-user isolation.</summary>
public sealed class OrderCollectionTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpClient> UserWithOrdersAsync(params decimal[] amounts)
    {
        var client = factory.CreateAuthenticatedClient($"user_{Guid.NewGuid():N}");
        foreach (var amount in amounts)
        {
            var response = await client.PostAsJsonAsync("/api/v1/orders", new { customerName = $"Customer {amount}", totalAmount = amount }, Ct);
            response.EnsureSuccessStatusCode();
        }
        return client;
    }

    private static Task<PagedResponse<OrderResponse>?> ListAsync(HttpClient client, string query = "") =>
        client.GetFromJsonAsync<PagedResponse<OrderResponse>>($"/api/v1/orders{query}", ApiJson.Options, Ct);

    [Fact]
    public async Task List_Defaults_NewestFirstWithPagingMetadata()
    {
        var client = await UserWithOrdersAsync(10, 20, 30);

        var page = await ListAsync(client);

        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(25, page.Limit);
        Assert.Equal(0, page.Offset);
        Assert.Equal([30m, 20m, 10m], page.Items.Select(o => o.TotalAmount));
    }

    [Fact]
    public async Task List_LimitAndOffset_ReturnRequestedPage()
    {
        var client = await UserWithOrdersAsync(10, 20, 30, 40, 50);

        var page = await ListAsync(client, "?limit=2&offset=2&sort=totalAmount");

        Assert.NotNull(page);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal([30m, 40m], page.Items.Select(o => o.TotalAmount));
    }

    [Fact]
    public async Task List_Filters_ByMinTotalAndStatus()
    {
        var client = await UserWithOrdersAsync(10, 50, 100);
        var all = await ListAsync(client);
        var hundred = all!.Items.Single(o => o.TotalAmount == 100);
        await client.PutAsJsonAsync($"/api/v1/orders/{hundred.Id}", new { customerName = hundred.CustomerName, totalAmount = 100, status = "Confirmed" }, Ct);

        var minTotal = await ListAsync(client, "?minTotal=50");
        var confirmed = await ListAsync(client, "?status=Confirmed");
        var confirmedLowercase = await ListAsync(client, "?status=confirmed");

        Assert.Equal([100m, 50m], minTotal!.Items.Select(o => o.TotalAmount));
        Assert.Equal(hundred.Id, Assert.Single(confirmed!.Items).Id);
        Assert.Equal(hundred.Id, Assert.Single(confirmedLowercase!.Items).Id);
    }

    [Fact]
    public async Task List_NoMatches_Returns200WithEmptyItems()
    {
        var client = await UserWithOrdersAsync(10);

        var page = await ListAsync(client, "?minTotal=1000");

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task List_OnlyReturnsCallersOrders()
    {
        await UserWithOrdersAsync(10, 20);
        var other = await UserWithOrdersAsync(99);

        var page = await ListAsync(other);

        Assert.Equal(99m, Assert.Single(page!.Items).TotalAmount);
    }

    [Theory]
    [InlineData("?limit=101")]
    [InlineData("?limit=0")]
    [InlineData("?offset=-1")]
    [InlineData("?sort=customerName")]
    [InlineData("?status=Shipped")]
    [InlineData("?status=1")]
    public async Task List_InvalidQuery_Returns400ValidationProblem(string query)
    {
        var client = await UserWithOrdersAsync();

        var response = await client.GetAsync($"/api/v1/orders{query}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.NotEmpty(problem!.Errors);
    }
}
