using Microsoft.EntityFrameworkCore;
using Tms.Execution.Domain.Entities;

namespace Tms.Execution.Application.Common.Interfaces;

public interface IExecutionDbContext
{
    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentItem> ShipmentItems { get; }
    DbSet<PODRecord> PODRecords { get; }
    DbSet<PODDocument> PODDocuments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
