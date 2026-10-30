using Template.Domain.Common;

namespace Template.Domain.Orders;

/// <summary>Sample aggregate. Replace with the product's own domain.</summary>
public sealed class Order : Entity
{
    public const int CustomerNameMaxLength = 200;
    public const int CreatedByMaxLength = 64;

    // Required by EF Core.
    private Order()
    {
    }

    public string CustomerName { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Id of the user who created the order (Clerk user id, e.g. <c>user_123</c>).</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new pending order. Guards enforce invariants; user-facing validation
    /// happens earlier in the Application layer.
    /// </summary>
    public static Order Create(string customerName, decimal totalAmount, string createdBy, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(customerName.Length, CustomerNameMaxLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalAmount);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(createdBy.Length, CreatedByMaxLength);

        return new Order
        {
            Id = Guid.CreateVersion7(createdAt),
            CustomerName = customerName.Trim(),
            TotalAmount = totalAmount,
            Status = OrderStatus.Pending,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
        };
    }

    /// <summary>
    /// Replaces the client-modifiable state (full update, PUT semantics). Idempotent: applying the
    /// same values again succeeds without changes.
    /// Rules: details can only change while <see cref="OrderStatus.Pending"/>; status may move
    /// Pending → Confirmed → Cancelled, or Pending → Cancelled.
    /// </summary>
    /// <returns><c>null</c> on success, otherwise the rule that was violated.</returns>
    public DomainError? Replace(string customerName, decimal totalAmount, OrderStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(customerName.Length, CustomerNameMaxLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalAmount);

        customerName = customerName.Trim();
        var detailsChanged = customerName != CustomerName || totalAmount != TotalAmount;

        if (detailsChanged && Status != OrderStatus.Pending)
        {
            return new DomainError("order_not_modifiable", $"Only pending orders can be changed. This order is {Status}.");
        }

        if (status != Status && !IsAllowedTransition(Status, status))
        {
            return new DomainError("invalid_status_transition", $"An order cannot move from {Status} to {status}.");
        }

        CustomerName = customerName;
        TotalAmount = totalAmount;
        Status = status;
        return null;
    }

    private static bool IsAllowedTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => true,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        _ => false,
    };
}
