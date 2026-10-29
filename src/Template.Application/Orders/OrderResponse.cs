using Template.Domain.Orders;

namespace Template.Application.Orders;

/// <summary>An order as returned by the API.</summary>
/// <param name="Id">Order identifier (UUID v7, time-ordered).</param>
/// <param name="CustomerName">Name of the customer who placed the order.</param>
/// <param name="TotalAmount">Total order amount.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="CreatedAt">When the order was created (UTC).</param>
public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt)
{
    internal static OrderResponse FromOrder(Order order) =>
        new(order.Id, order.CustomerName, order.TotalAmount, order.Status, order.CreatedAt);
}
