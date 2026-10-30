using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Template.Application.Common;
using Template.Application.Orders;
using Template.Application.Orders.CreateOrder;
using Template.Application.Orders.DeleteOrder;
using Template.Application.Orders.GetOrder;
using Template.Application.Orders.ListOrders;
using Template.Application.Orders.UpdateOrder;
using Template.Domain.Orders;

namespace Template.Api.Endpoints.Orders;

/// <summary>
/// /orders is the collection, /orders/{id} an item. Verbs carry the action (no /create-order style URIs).
/// Status codes follow RFC 9110: 200, 201 + Location, 204, 400, 404, 409, 415 (non-JSON body).
/// </summary>
internal static class OrderEndpoints
{
    private const string GetOrderRouteName = "GetOrder";

    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/orders").WithTags("Orders");

        group.MapGet("/", ListOrdersAsync)
            .WithName("ListOrders")
            .WithSummary("List orders")
            .WithDescription("Returns the caller's orders, newest first by default. Supports filtering, sorting and limit/offset pagination.");

        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("Create an order")
            .WithDescription("Creates a new order in the Pending state. The Location header holds the URI of the new order.");

        group.MapGet("/{id:guid}", GetOrderAsync)
            .WithName(GetOrderRouteName)
            .WithSummary("Get an order by id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateOrderAsync)
            .WithName("UpdateOrder")
            .WithSummary("Replace an order")
            .WithDescription("Full update (idempotent). Details can only change while Pending; status may move Pending → Confirmed → Cancelled or Pending → Cancelled. Violations return 409.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteOrderAsync)
            .WithName("DeleteOrder")
            .WithSummary("Delete an order")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Ok<PagedResponse<OrderResponse>>, ValidationProblem, ProblemHttpResult>> ListOrdersAsync(
        [AsParameters] ListOrdersRequest request,
        ListOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        if (!request.TryGetStatus(out var status))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = [$"Status must be one of: {string.Join(", ", Enum.GetNames<OrderStatus>())}."],
            });
        }

        var query = new ListOrdersQuery(
            request.Limit ?? Paging.DefaultLimit,
            request.Offset ?? 0,
            status,
            request.MinTotal,
            request.Sort ?? OrderSort.Default);

        var result = await handler.HandleAsync(query, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.ToFailureResult<Ok<PagedResponse<OrderResponse>>>();
    }

    private static async Task<Results<CreatedAtRoute<OrderResponse>, ValidationProblem, ProblemHttpResult>> CreateOrderAsync(
        CreateOrderCommand command,
        CreateOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, GetOrderRouteName, new { id = result.Value.Id })
            : result.Error.ToFailureResult<CreatedAtRoute<OrderResponse>>();
    }

    private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> GetOrderAsync(
        [Description("The order id.")] Guid id,
        GetOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOrderQuery(id), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<OrderResponse>, ValidationProblem, ProblemHttpResult>> UpdateOrderAsync(
        [Description("The order id.")] Guid id,
        UpdateOrderCommand command,
        UpdateOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.ToFailureResult<Ok<OrderResponse>>();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteOrderAsync(
        [Description("The order id.")] Guid id,
        DeleteOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.ToProblem();
    }
}
