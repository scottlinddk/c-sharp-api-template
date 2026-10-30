using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Template.Application.Abstractions;
using Template.Application.Common;

namespace Template.Application.Orders.UpdateOrder;

public sealed class UpdateOrderHandler(IAppDbContext db, IValidator<UpdateOrderCommand> validator, ICurrentUser currentUser)
{
    public async Task<Result<OrderResponse>> HandleAsync(Guid id, UpdateOrderCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.ToDictionary());
        }

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.CreatedBy == currentUser.UserId, cancellationToken);
        if (order is null)
        {
            return Error.NotFound("order_not_found", $"Order '{id}' was not found.");
        }

        if (order.Replace(command.CustomerName, command.TotalAmount, command.Status) is { } conflict)
        {
            return Error.Conflict(conflict.Code, conflict.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return OrderResponse.FromOrder(order);
    }
}
