using FluentValidation;
using Tms.Orders.Domain.Interfaces;

namespace Tms.Orders.Application.Features.SplitOrder;

public sealed class SplitOrderCommandValidator : AbstractValidator<SplitOrderCommand>
{
    public SplitOrderCommandValidator(IOrderRepository repo)
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.Parts)
            .NotNull()
            .Must(p => p.Count >= 2)
            .WithMessage("Split must produce at least 2 parts.");

        RuleForEach(x => x.Parts).ChildRules(part =>
        {
            part.RuleFor(p => p.Items)
                .NotNull()
                .Must(items => items.Count > 0)
                .WithMessage("Each part must have at least one item allocation.");

            part.RuleForEach(p => p.Items).ChildRules(alloc =>
            {
                alloc.RuleFor(a => a.ItemId).NotEmpty();
                alloc.RuleFor(a => a.Quantity)
                    .GreaterThan(0).WithMessage("Allocated quantity must be positive.");
            });
        });

        // Cross-field validation: each ItemId must exist in the parent order
        // and total allocated qty per item must not exceed original qty
        // (Done in Handler after loading the parent from DB)
    }
}

public sealed class AutoSplitOrderCommandValidator : AbstractValidator<AutoSplitOrderCommand>
{
    public AutoSplitOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.MaxWeightPerSplitKg)
            .GreaterThan(0).WithMessage("MaxWeightPerSplitKg must be greater than 0.");

        RuleFor(x => x.MaxVolumePerSplitCBM)
            .GreaterThanOrEqualTo(0).WithMessage("MaxVolumePerSplitCBM must be >= 0.");
    }
}
