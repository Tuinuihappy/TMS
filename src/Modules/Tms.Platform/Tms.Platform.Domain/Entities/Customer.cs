using Tms.SharedKernel.Domain;

namespace Tms.Platform.Domain.Entities;

/// <summary>Customer master data — CRUD entity (not rich domain)</summary>
public sealed class Customer : AggregateRoot
{
    public string CustomerCode { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? TaxId { get; private set; }
    public string? PaymentTerms { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public Guid TenantId { get; private set; }

    private Customer() { }

    public static Customer Create(
        string customerCode, string companyName, Guid tenantId,
        string? contactPerson = null, string? phone = null,
        string? email = null, string? taxId = null, string? paymentTerms = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);
        return new Customer
        {
            CustomerCode = customerCode, CompanyName = companyName,
            TenantId = tenantId, ContactPerson = contactPerson,
            Phone = phone, Email = email, TaxId = taxId,
            PaymentTerms = paymentTerms, IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string companyName, string? contactPerson, string? phone,
        string? email, string? taxId, string? paymentTerms)
    {
        CompanyName = companyName;
        ContactPerson = contactPerson;
        Phone = phone; Email = email;
        TaxId = taxId; PaymentTerms = paymentTerms;
    }

    public void Deactivate() => IsActive = false;
}
