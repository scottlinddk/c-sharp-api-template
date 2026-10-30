using Template.Domain.Orders;

namespace Template.Domain.Tests.Orders;

public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInput_ReturnsPendingOrder()
    {
        var order = Order.Create("  Acme A/S ", 125.50m, "user_123", Now);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("Acme A/S", order.CustomerName);
        Assert.Equal(125.50m, order.TotalAmount);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(Now, order.CreatedAt);
        Assert.Equal("user_123", order.CreatedBy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveAmount_Throws(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Order.Create("Acme", amount, "user_123", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankCustomerName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => Order.Create(name, 10m, "user_123", Now));
    }

    [Fact]
    public void Create_TooLongCustomerName_Throws()
    {
        var name = new string('x', Order.CustomerNameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => Order.Create(name, 10m, "user_123", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_BlankCreatedBy_Throws(string createdBy)
    {
        Assert.Throws<ArgumentException>(() => Order.Create("Acme", 10m, createdBy, Now));
    }

    [Fact]
    public void Replace_PendingOrder_UpdatesDetailsAndStatus()
    {
        var order = Order.Create("Acme", 10m, "user_123", Now);

        var error = order.Replace(" Acme Nordic ", 20m, OrderStatus.Confirmed);

        Assert.Null(error);
        Assert.Equal("Acme Nordic", order.CustomerName);
        Assert.Equal(20m, order.TotalAmount);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Replace_SameValuesTwice_IsIdempotent()
    {
        var order = Order.Create("Acme", 10m, "user_123", Now);
        Assert.Null(order.Replace("Acme", 10m, OrderStatus.Cancelled));

        var second = order.Replace("Acme", 10m, OrderStatus.Cancelled);

        Assert.Null(second);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Replace_ChangeDetailsWhenNotPending_ReturnsConflict()
    {
        var order = Order.Create("Acme", 10m, "user_123", Now);
        order.Replace("Acme", 10m, OrderStatus.Confirmed);

        var error = order.Replace("Other", 10m, OrderStatus.Confirmed);

        Assert.Equal("order_not_modifiable", error?.Code);
        Assert.Equal("Acme", order.CustomerName);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    public void Replace_InvalidTransition_ReturnsConflict(OrderStatus from, OrderStatus to)
    {
        var order = Order.Create("Acme", 10m, "user_123", Now);
        if (from == OrderStatus.Cancelled)
        {
            order.Replace("Acme", 10m, OrderStatus.Confirmed);
        }
        order.Replace("Acme", 10m, from);

        var error = order.Replace("Acme", 10m, to);

        Assert.Equal("invalid_status_transition", error?.Code);
        Assert.Equal(from, order.Status);
    }
}
