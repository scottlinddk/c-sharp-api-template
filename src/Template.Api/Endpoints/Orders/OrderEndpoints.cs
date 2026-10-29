using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Template.Application.Common;
using Template.Application.Orders;
using Template.Application.Orders.CreateOrder;
using Template.Application.Orders.GetOrder;

namespace Template.Api.Endpoints.Orders;

internal static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/orders").WithTags("Orders");

        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("Create an order")
            .WithDescription("Creates a new order in the Pending state.");

        group.MapGet("/{id:guid}", GetOrderAsync)
            .WithName("GetOrder")
            .WithSummary("Get an order by id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Created<OrderResponse>, ValidationProblem, ProblemHttpResult>> CreateOrderAsync(
        CreateOrderCommand command,
        CreateOrderHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.Validation
                ? TypedResults.ValidationProblem(result.Error.ValidationErrors ?? new Dictionary<string, string[]>())
                : result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/v1/orders/{result.Value.Id}", result.Value);
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
}
