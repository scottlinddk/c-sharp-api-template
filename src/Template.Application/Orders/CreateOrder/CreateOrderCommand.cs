namespace Template.Application.Orders.CreateOrder;

/// <summary>Request to create a new order.</summary>
/// <param name="CustomerName">Name of the customer. Required, max 200 characters.</param>
/// <param name="TotalAmount">Total order amount. Must be greater than zero.</param>
public sealed record CreateOrderCommand(string CustomerName, decimal TotalAmount);
