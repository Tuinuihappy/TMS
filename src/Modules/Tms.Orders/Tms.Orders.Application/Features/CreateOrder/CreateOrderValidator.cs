using FluentValidation;

namespace Tms.Orders.Application.Features.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.PickupAddress).NotNull();
        RuleFor(x => x.PickupAddress.Street).NotEmpty().WithMessage("Pickup street is required.");
        RuleFor(x => x.PickupAddress.Province).NotEmpty().WithMessage("Pickup province is required.");

        RuleFor(x => x.DropoffAddress).NotNull();
        RuleFor(x => x.DropoffAddress.Street).NotEmpty().WithMessage("Dropoff street is required.");
        RuleFor(x => x.DropoffAddress.Province).NotEmpty().WithMessage("Dropoff province is required.");

        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty();
            item.RuleFor(i => i.Weight).GreaterThan(0).WithMessage("Weight must be greater than 0.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        });

        When(x => x.PickupWindow is not null, () =>
            RuleFor(x => x.PickupWindow!.To)
                .GreaterThan(x => x.PickupWindow!.From)
                .WithMessage("Pickup window 'To' must be after 'From'."));

        When(x => x.DropoffWindow is not null, () =>
            RuleFor(x => x.DropoffWindow!.To)
                .GreaterThan(x => x.DropoffWindow!.From)
                .WithMessage("Dropoff window 'To' must be after 'From'."));
    }
}
