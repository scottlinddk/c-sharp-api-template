using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Template.Application.Abstractions;
using Template.Application.Common;

namespace Template.Application.Orders.ListOrders;

/// <summary>Lists the caller's orders with filtering, sorting and limit/offset pagination.</summary>
public sealed class ListOrdersHandler(IAppDbContext db, IValidator<ListOrdersQuery> validator, ICurrentUser currentUser)
{
    public async Task<Result<PagedResponse<OrderResponse>>> HandleAsync(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.ToDictionary());
        }

        var orders = db.Orders.AsNoTracking().Where(o => o.CreatedBy == currentUser.UserId);

        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        if (query.MinTotal is { } minTotal)
        {
            orders = orders.Where(o => o.TotalAmount >= minTotal);
        }

        var totalCount = await orders.CountAsync(cancellationToken);

        var items = await OrderSort.Apply(orders, query.Sort)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(o => new OrderResponse(o.Id, o.CustomerName, o.TotalAmount, o.Status, o.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderResponse>(items, query.Offset, query.Limit, totalCount);
    }
}
