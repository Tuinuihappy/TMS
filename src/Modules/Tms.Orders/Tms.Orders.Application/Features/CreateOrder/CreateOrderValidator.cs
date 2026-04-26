using FluentValidation;

namespace Tms.Orders.Application.Features.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Stops)
            .NotEmpty().WithMessage("Order must have at least one stop.");

        RuleForEach(x => x.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.PickupAddress).NotNull();
            stop.RuleFor(s => s.PickupAddress.Street).NotEmpty().WithMessage("Pickup street is required.");
            stop.RuleFor(s => s.PickupAddress.Province).NotEmpty().WithMessage("Pickup province is required.");

            stop.RuleFor(s => s.DropoffAddress).NotNull();
            stop.RuleFor(s => s.DropoffAddress.Street).NotEmpty().WithMessage("Dropoff street is required.");
            stop.RuleFor(s => s.DropoffAddress.Province).NotEmpty().WithMessage("Dropoff province is required.");

            stop.RuleFor(s => s.Items).NotEmpty().WithMessage("Each stop must have at least one item.");
            stop.RuleForEach(s => s.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.Description).NotEmpty();
                item.RuleFor(i => i.WeightKg).GreaterThan(0).WithMessage("Weight must be greater than 0.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            });

            stop.When(s => s.PickupWindow is not null, () =>
                stop.RuleFor(s => s.PickupWindow!.To)
                    .GreaterThan(s => s.PickupWindow!.From)
                    .WithMessage("Pickup window 'To' must be after 'From'."));

            stop.When(s => s.DropoffWindow is not null, () =>
                stop.RuleFor(s => s.DropoffWindow!.To)
                    .GreaterThan(s => s.DropoffWindow!.From)
                    .WithMessage("Dropoff window 'To' must be after 'From'."));
        });
    }
}
