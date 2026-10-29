using FluentValidation;
using Template.Application.Abstractions;
using Template.Application.Common;
using Template.Domain.Orders;

namespace Template.Application.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    IAppDbContext db,
    IValidator<CreateOrderCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<Result<OrderResponse>> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.ToDictionary());
        }

        var order = Order.Create(command.CustomerName, command.TotalAmount, timeProvider.GetUtcNow());

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return OrderResponse.FromOrder(order);
    }
}
