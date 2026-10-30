using Template.Domain.Orders;

namespace Template.Application.Orders.UpdateOrder;

/// <summary>Full replacement of an order's modifiable fields (PUT semantics).</summary>
/// <param name="CustomerName">Name of the customer. Required, max 200 characters.</param>
/// <param name="TotalAmount">Total order amount. Must be greater than zero.</param>
/// <param name="Status">Target status. Allowed transitions: Pending → Confirmed → Cancelled, Pending → Cancelled.</param>
public sealed record UpdateOrderCommand(string CustomerName, decimal TotalAmount, OrderStatus Status);
