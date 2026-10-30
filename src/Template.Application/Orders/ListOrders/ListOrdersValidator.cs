using FluentValidation;
using Template.Application.Common;

namespace Template.Application.Orders.ListOrders;

public sealed class ListOrdersValidator : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, Paging.MaxLimit);
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.MinTotal).GreaterThanOrEqualTo(0).When(x => x.MinTotal is not null);
        RuleFor(x => x.Sort)
            .Must(OrderSort.IsSupported)
            .WithMessage($"Sort must be one of: {string.Join(", ", OrderSort.Supported)}.");
    }
}
