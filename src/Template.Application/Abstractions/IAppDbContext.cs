using Microsoft.EntityFrameworkCore;
using Template.Domain.Orders;

namespace Template.Application.Abstractions;

/// <summary>
/// Persistence port used by handlers. Exposing <see cref="DbSet{TEntity}"/> is a deliberate,
/// pragmatic trade-off: it couples Application to EF Core, but avoids a repository layer
/// that would only wrap EF Core again.
/// </summary>
public interface IAppDbContext
{
    DbSet<Order> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
