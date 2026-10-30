using Microsoft.EntityFrameworkCore;
using Template.Application.Abstractions;
using Template.Application.Common;

namespace Template.Application.Orders.DeleteOrder;

public sealed class DeleteOrderHandler(IAppDbContext db, ICurrentUser currentUser)
{
    /// <returns>The deleted id, or not found (also for orders owned by other users).</returns>
    public async Task<Result<Guid>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await db.Orders
            .Where(o => o.Id == id && o.CreatedBy == currentUser.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0
            ? Error.NotFound("order_not_found", $"Order '{id}' was not found.")
            : id;
    }
}
