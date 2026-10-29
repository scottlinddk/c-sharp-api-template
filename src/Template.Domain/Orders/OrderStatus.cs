namespace Template.Domain.Orders;

/// <summary>Lifecycle state of an order.</summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
}
