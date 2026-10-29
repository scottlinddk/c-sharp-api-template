using Template.Application.Orders.CreateOrder;
using Template.Domain.Orders;

namespace Template.Application.Tests.Orders;

public sealed class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new CreateOrderCommand("Acme A/S", 10m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCustomerName_HasError()
    {
        var result = _validator.Validate(new CreateOrderCommand("", 10m));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.CustomerName));
    }

    [Fact]
    public void Validate_TooLongCustomerName_HasError()
    {
        var name = new string('x', Order.CustomerNameMaxLength + 1);

        var result = _validator.Validate(new CreateOrderCommand(name, 10m));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.CustomerName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveAmount_HasError(decimal amount)
    {
        var result = _validator.Validate(new CreateOrderCommand("Acme", amount));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.TotalAmount));
    }
}
