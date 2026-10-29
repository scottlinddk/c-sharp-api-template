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
}
