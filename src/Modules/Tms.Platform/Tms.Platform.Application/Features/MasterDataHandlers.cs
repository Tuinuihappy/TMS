using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Enums;
using Tms.Platform.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Platform.Application.Features.MasterData;

// ════════════════════════════════════════════════════════════════════════════
// SHARED DTOs
// ════════════════════════════════════════════════════════════════════════════

public sealed record CustomerDto(
    Guid Id, string CustomerCode, string CompanyName,
    string? ContactPerson, string? Phone, string? Email,
    string? TaxId, string? PaymentTerms, bool IsActive, DateTime CreatedAt);

public sealed record LocationDto(
    Guid Id, string LocationCode, string Name, string? AddressLine,
    string? District, string? Province, string? PostalCode,
    double Latitude, double Longitude, string? Zone, string Type,
    Guid? CustomerId, bool IsActive);

public sealed record ReasonCodeDto(
    Guid Id, string Code, string Description, string Category, bool IsActive);

public sealed record ProvinceDto(int Id, string NameTH, string? NameEN, string? Region);

public sealed record HolidayDto(Guid Id, DateTime Date, string Description, int Year);

// ════════════════════════════════════════════════════════════════════════════
// CUSTOMER
// ════════════════════════════════════════════════════════════════════════════

public sealed record CreateCustomerCommand(
    string CustomerCode, string CompanyName, Guid TenantId,
    string? ContactPerson = null, string? Phone = null,
    string? Email = null, string? TaxId = null, string? PaymentTerms = null
) : ICommand<Guid>;

public sealed class CreateCustomerHandler(ICustomerRepository repo)
    : ICommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand req, CancellationToken ct)
    {
        if (await repo.ExistsByCodeAsync(req.CustomerCode, req.TenantId, ct))
            throw new DomainException($"CustomerCode '{req.CustomerCode}' already exists.", "DUPLICATE_CUSTOMER_CODE");

        var customer = Customer.Create(
            req.CustomerCode, req.CompanyName, req.TenantId,
            req.ContactPerson, req.Phone, req.Email, req.TaxId, req.PaymentTerms);

        await repo.AddAsync(customer, ct);
        return customer.Id;
    }
}

public sealed record UpdateCustomerCommand(
    Guid CustomerId, string CompanyName,
    string? ContactPerson, string? Phone,
    string? Email, string? TaxId, string? PaymentTerms) : ICommand;

public sealed class UpdateCustomerHandler(ICustomerRepository repo)
    : ICommandHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand req, CancellationToken ct)
    {
        var customer = await repo.GetByIdAsync(req.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), req.CustomerId);
        customer.Update(req.CompanyName, req.ContactPerson, req.Phone, req.Email, req.TaxId, req.PaymentTerms);
        await repo.UpdateAsync(customer, ct);
    }
}

public sealed record DeactivateCustomerCommand(Guid CustomerId) : ICommand;
public sealed class DeactivateCustomerHandler(ICustomerRepository repo)
    : ICommandHandler<DeactivateCustomerCommand>
{
    public async Task Handle(DeactivateCustomerCommand req, CancellationToken ct)
    {
        var customer = await repo.GetByIdAsync(req.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), req.CustomerId);
        customer.Deactivate();
        await repo.UpdateAsync(customer, ct);
    }
}

public sealed record GetCustomersQuery(
    int Page = 1, int PageSize = 20, bool? IsActive = null, Guid? TenantId = null
) : IQuery<PagedResult<CustomerDto>>;

public sealed class GetCustomersHandler(ICustomerRepository repo)
    : IQueryHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery req, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(req.Page, req.PageSize, req.IsActive, req.TenantId, ct);
        return PagedResult<CustomerDto>.Create(items.Select(MapDto).ToList(), total, req.Page, req.PageSize);
    }

    internal static CustomerDto MapDto(Customer c) => new(
        c.Id, c.CustomerCode, c.CompanyName, c.ContactPerson,
        c.Phone, c.Email, c.TaxId, c.PaymentTerms, c.IsActive, c.CreatedAt);
}

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDto?>;
public sealed class GetCustomerByIdHandler(ICustomerRepository repo)
    : IQueryHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery req, CancellationToken ct)
    {
        var c = await repo.GetByIdAsync(req.CustomerId, ct);
        return c is null ? null : GetCustomersHandler.MapDto(c);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// LOCATION
// ════════════════════════════════════════════════════════════════════════════

public sealed record CreateLocationCommand(
    string LocationCode, string Name, double Latitude, double Longitude,
    string Type, Guid TenantId,
    string? AddressLine = null, string? District = null,
    string? Province = null, string? PostalCode = null,
    string? Zone = null, Guid? CustomerId = null) : ICommand<Guid>;

public sealed class CreateLocationHandler(ILocationRepository repo)
    : ICommandHandler<CreateLocationCommand, Guid>
{
    public async Task<Guid> Handle(CreateLocationCommand req, CancellationToken ct)
    {
        if (await repo.ExistsByCodeAsync(req.LocationCode, req.TenantId, ct))
            throw new DomainException($"LocationCode '{req.LocationCode}' already exists.", "DUPLICATE_LOCATION_CODE");

        var locType = Enum.Parse<LocationType>(req.Type, ignoreCase: true);
        var location = Location.Create(
            req.LocationCode, req.Name, req.Latitude, req.Longitude, locType, req.TenantId,
            req.AddressLine, req.District, req.Province, req.PostalCode, req.Zone, req.CustomerId);

        await repo.AddAsync(location, ct);
        return location.Id;
    }
}

public sealed record UpdateLocationCommand(
    Guid LocationId, string Name, string? AddressLine,
    string? Province, string? Zone,
    double Latitude, double Longitude) : ICommand;

public sealed class UpdateLocationHandler(ILocationRepository repo)
    : ICommandHandler<UpdateLocationCommand>
{
    public async Task Handle(UpdateLocationCommand req, CancellationToken ct)
    {
        var loc = await repo.GetByIdAsync(req.LocationId, ct)
            ?? throw new NotFoundException(nameof(Location), req.LocationId);
        loc.Update(req.Name, req.AddressLine, req.Province, req.Zone, req.Latitude, req.Longitude);
        await repo.UpdateAsync(loc, ct);
    }
}

public sealed record GetLocationsQuery(
    int Page = 1, int PageSize = 20,
    string? Type = null, string? Zone = null,
    Guid? CustomerId = null, Guid? TenantId = null
) : IQuery<PagedResult<LocationDto>>;

public sealed class GetLocationsHandler(ILocationRepository repo)
    : IQueryHandler<GetLocationsQuery, PagedResult<LocationDto>>
{
    public async Task<PagedResult<LocationDto>> Handle(GetLocationsQuery req, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            req.Page, req.PageSize, req.Type, req.Zone, req.CustomerId, req.TenantId, ct);
        return PagedResult<LocationDto>.Create(items.Select(MapDto).ToList(), total, req.Page, req.PageSize);
    }

    internal static LocationDto MapDto(Location l) => new(
        l.Id, l.LocationCode, l.Name, l.AddressLine, l.District,
        l.Province, l.PostalCode, l.Latitude, l.Longitude, l.Zone,
        l.Type.ToString(), l.CustomerId, l.IsActive);
}

public sealed record SearchLocationsQuery(
    string Query, string? Type = null, Guid? TenantId = null, int MaxResults = 20
) : IQuery<List<LocationDto>>;

public sealed class SearchLocationsHandler(ILocationRepository repo)
    : IQueryHandler<SearchLocationsQuery, List<LocationDto>>
{
    public async Task<List<LocationDto>> Handle(SearchLocationsQuery req, CancellationToken ct)
    {
        var items = await repo.SearchAsync(req.Query, req.Type, req.TenantId, req.MaxResults, ct);
        return items.Select(GetLocationsHandler.MapDto).ToList();
    }
}

// ════════════════════════════════════════════════════════════════════════════
// REASON CODES
// ════════════════════════════════════════════════════════════════════════════

public sealed record CreateReasonCodeCommand(
    string Code, string Description, string Category, Guid TenantId) : ICommand<Guid>;

public sealed class CreateReasonCodeHandler(IReasonCodeRepository repo)
    : ICommandHandler<CreateReasonCodeCommand, Guid>
{
    public async Task<Guid> Handle(CreateReasonCodeCommand req, CancellationToken ct)
    {
        var cat = Enum.Parse<ReasonCategory>(req.Category, ignoreCase: true);
        var rc = ReasonCode.Create(req.Code, req.Description, cat, req.TenantId);
        await repo.AddAsync(rc, ct);
        return rc.Id;
    }
}

public sealed record GetReasonCodesQuery(
    string? Category = null, Guid? TenantId = null) : IQuery<List<ReasonCodeDto>>;

public sealed class GetReasonCodesHandler(IReasonCodeRepository repo)
    : IQueryHandler<GetReasonCodesQuery, List<ReasonCodeDto>>
{
    public async Task<List<ReasonCodeDto>> Handle(GetReasonCodesQuery req, CancellationToken ct)
    {
        var items = await repo.GetByCategoryAsync(req.Category, req.TenantId ?? Guid.Empty, ct);
        return items.Select(r => new ReasonCodeDto(r.Id, r.Code, r.Description, r.Category.ToString(), r.IsActive)).ToList();
    }
}

// ════════════════════════════════════════════════════════════════════════════
// PROVINCES + HOLIDAYS
// ════════════════════════════════════════════════════════════════════════════

public sealed record GetProvincesQuery : IQuery<List<ProvinceDto>>;
public sealed class GetProvincesHandler(IProvinceRepository repo)
    : IQueryHandler<GetProvincesQuery, List<ProvinceDto>>
{
    public async Task<List<ProvinceDto>> Handle(GetProvincesQuery req, CancellationToken ct)
    {
        var items = await repo.GetAllAsync(ct);
        return items.Select(p => new ProvinceDto(p.Id, p.NameTH, p.NameEN, p.Region)).ToList();
    }
}

public sealed record GetHolidaysQuery(int Year, Guid? TenantId = null) : IQuery<List<HolidayDto>>;
public sealed class GetHolidaysHandler(IHolidayRepository repo)
    : IQueryHandler<GetHolidaysQuery, List<HolidayDto>>
{
    public async Task<List<HolidayDto>> Handle(GetHolidaysQuery req, CancellationToken ct)
    {
        var items = await repo.GetByYearAsync(req.Year, req.TenantId ?? Guid.Empty, ct);
        return items.Select(h => new HolidayDto(h.Id, h.Date, h.Description, h.Year)).ToList();
    }
}
