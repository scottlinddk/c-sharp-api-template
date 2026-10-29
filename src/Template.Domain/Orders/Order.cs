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
}
