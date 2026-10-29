using Microsoft.EntityFrameworkCore;
using Template.Application.Abstractions;
using Template.Application.Common;

namespace Template.Application.Orders.GetOrder;

/// <summary>
/// Returns the order only if the caller created it. Other users get "not found" rather than
/// "forbidden", so ids of other users' orders cannot be probed.
/// </summary>
public sealed class GetOrderHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<OrderResponse>> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.Id && o.CreatedBy == currentUser.UserId)
            .Select(o => new OrderResponse(o.Id, o.CustomerName, o.TotalAmount, o.Status, o.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return order is null
            ? Error.NotFound("order_not_found", $"Order '{query.Id}' was not found.")
            : order;
    }
}
