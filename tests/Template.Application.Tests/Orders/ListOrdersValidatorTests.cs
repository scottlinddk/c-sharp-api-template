using Template.Application.Common;
using Template.Application.Orders.ListOrders;

namespace Template.Application.Tests.Orders;

public sealed class ListOrdersValidatorTests
{
    private readonly ListOrdersValidator _validator = new();

    private static ListOrdersQuery Query(int limit = Paging.DefaultLimit, int offset = 0, decimal? minTotal = null, string sort = OrderSort.Default) =>
        new(limit, offset, null, minTotal, sort);

    [Fact]
    public void Validate_Defaults_IsValid() => Assert.True(_validator.Validate(Query()).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(Paging.MaxLimit + 1)]
    public void Validate_LimitOutOfRange_HasError(int limit) =>
        Assert.Contains(_validator.Validate(Query(limit: limit)).Errors, e => e.PropertyName == nameof(ListOrdersQuery.Limit));

    [Fact]
    public void Validate_NegativeOffset_HasError() =>
        Assert.Contains(_validator.Validate(Query(offset: -1)).Errors, e => e.PropertyName == nameof(ListOrdersQuery.Offset));

    [Fact]
    public void Validate_NegativeMinTotal_HasError() =>
        Assert.Contains(_validator.Validate(Query(minTotal: -1)).Errors, e => e.PropertyName == nameof(ListOrdersQuery.MinTotal));

    [Theory]
    [InlineData("customerName")]
    [InlineData("createdAt; DROP TABLE orders")]
    [InlineData("")]
    public void Validate_UnsupportedSort_HasError(string sort) =>
        Assert.Contains(_validator.Validate(Query(sort: sort)).Errors, e => e.PropertyName == nameof(ListOrdersQuery.Sort));
}
