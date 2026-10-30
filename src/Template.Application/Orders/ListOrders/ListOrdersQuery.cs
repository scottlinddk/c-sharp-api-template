using Template.Domain.Orders;

namespace Template.Application.Orders.ListOrders;

/// <param name="Limit">Page size, 1 to <see cref="Common.Paging.MaxLimit"/>.</param>
/// <param name="Offset">Number of items to skip.</param>
/// <param name="Status">Only orders with this status.</param>
/// <param name="MinTotal">Only orders with at least this total amount.</param>
/// <param name="Sort">Sort field, optionally prefixed with '-' for descending.</param>
public sealed record ListOrdersQuery(int Limit, int Offset, OrderStatus? Status, decimal? MinTotal, string Sort);
