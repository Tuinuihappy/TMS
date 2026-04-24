using FluentValidation;

namespace Tms.Execution.Application.Features.UpdateShipmentStatus;

public sealed class PickUpShipmentValidator : AbstractValidator<PickUpShipmentCommand>
{
    public PickUpShipmentValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}

public sealed class ArriveShipmentValidator : AbstractValidator<ArriveShipmentCommand>
{
    public ArriveShipmentValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}

public sealed class DeliverShipmentValidator : AbstractValidator<DeliverShipmentCommand>
{
    public DeliverShipmentValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one delivery item is required.");
        RuleFor(x => x.Pod).NotNull().WithMessage("Proof of delivery is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ShipmentItemId).NotEmpty();
            item.RuleFor(i => i.DeliveredQty).GreaterThan(0).WithMessage("Delivered quantity must be greater than 0.");
        });
    }
}

public sealed class RejectShipmentValidator : AbstractValidator<RejectShipmentCommand>
{
    public RejectShipmentValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Rejection reason is required.");
        RuleFor(x => x.ReasonCode).NotEmpty().WithMessage("Rejection reason code is required.");
    }
}

public sealed class RecordShipmentExceptionValidator : AbstractValidator<RecordShipmentExceptionCommand>
{
    public RecordShipmentExceptionValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Exception reason is required.");
        RuleFor(x => x.ReasonCode).NotEmpty().WithMessage("Exception reason code is required.");
    }
}

public sealed class ApprovePodValidator : AbstractValidator<ApprovePodCommand>
{
    public ApprovePodValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.ApprovedBy).NotEmpty().WithMessage("ApprovedBy user ID is required.");
    }
}
