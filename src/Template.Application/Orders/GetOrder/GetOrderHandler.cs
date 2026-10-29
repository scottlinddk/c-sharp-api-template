using Microsoft.EntityFrameworkCore;
using Template.Application.Abstractions;
using Template.Application.Common;

namespace Template.Application.Orders.GetOrder;

public sealed class GetOrderHandler(IAppDbContext db)
{
    public async Task<Result<OrderResponse>> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.Id)
            .Select(o => new OrderResponse(o.Id, o.CustomerName, o.TotalAmount, o.Status, o.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return order is null
            ? Error.NotFound("order_not_found", $"Order '{query.Id}' was not found.")
            : order;
    }
}
