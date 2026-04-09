using Microsoft.EntityFrameworkCore;
using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Application.Common.Interfaces;

public interface IPlanningDbContext
{
    DbSet<Trip> Trips { get; }
    DbSet<Stop> Stops { get; }
    DbSet<RoutePlan> RoutePlans { get; }
    DbSet<RouteStop> RouteStops { get; }
    DbSet<OptimizationRequest> OptimizationRequests { get; }
    DbSet<PlanningOrder> PlanningOrders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
