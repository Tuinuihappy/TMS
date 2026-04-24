using FluentValidation;
using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Application.Features;

public sealed class CreateTripValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.PlannedDate)
            .GreaterThan(DateTime.UtcNow.AddDays(-1))
            .WithMessage("PlannedDate cannot be more than 1 day in the past.");
    }
}

public sealed class AssignResourcesValidator : AbstractValidator<AssignResourcesCommand>
{
    public AssignResourcesValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("VehicleId is required.");
        RuleFor(x => x.DriverId).NotEmpty().WithMessage("DriverId is required.");
    }
}

public sealed class DispatchTripValidator : AbstractValidator<DispatchTripCommand>
{
    public DispatchTripValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
    }
}

public sealed class CompleteTripValidator : AbstractValidator<CompleteTripCommand>
{
    public CompleteTripValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
    }
}

public sealed class CancelTripValidator : AbstractValidator<CancelTripCommand>
{
    public CancelTripValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Cancel reason is required.");
    }
}

public sealed class AddStopValidator : AbstractValidator<AddStopCommand>
{
    private static readonly HashSet<string> ValidTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Pickup", "Dropoff", "Return" };

    public AddStopValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.Stop.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.Stop.Sequence).GreaterThan(0).WithMessage("Sequence must be greater than 0.");
        RuleFor(x => x.Stop.Type)
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("Stop type must be Pickup, Dropoff, or Return.");
    }
}
