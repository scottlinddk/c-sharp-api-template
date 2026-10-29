using FluentValidation;
using Template.Domain.Orders;

namespace Template.Application.Orders.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(Order.CustomerNameMaxLength);

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0);
    }
}
