using Template.Domain.Orders;

namespace Template.Application.Orders.ListOrders;

/// <summary>Allow-listed sort options. Sorting on arbitrary client input is never passed to the database.</summary>
public static class OrderSort
{
    public const string Default = "-createdAt";

    public static readonly IReadOnlyList<string> Supported = ["createdAt", "-createdAt", "totalAmount", "-totalAmount"];

    public static bool IsSupported(string? sort) => sort is not null && Supported.Contains(sort);

    /// <summary>Applies the sort with the order id as tie-breaker, so pages are stable.</summary>
    public static IOrderedQueryable<Order> Apply(IQueryable<Order> query, string sort) => sort switch
    {
        "createdAt" => query.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id),
        "totalAmount" => query.OrderBy(o => o.TotalAmount).ThenBy(o => o.Id),
        "-totalAmount" => query.OrderByDescending(o => o.TotalAmount).ThenByDescending(o => o.Id),
        _ => query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id),
    };
}
