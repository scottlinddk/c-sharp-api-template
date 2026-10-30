using FluentValidation;
using Template.Domain.Orders;

namespace Template.Application.Orders.UpdateOrder;

public sealed class UpdateOrderValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(Order.CustomerNameMaxLength);
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum();
    }
}
