using CurrencyTracker.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IUnitOfWork"/>. Delegates directly
/// to <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>;
/// the implicit transaction EF Core opens around <c>SaveChangesAsync</c>
/// is the transaction boundary. Phase 12's Wolverine outbox will
/// replace this adapter with one that bundles outgoing messages
/// into the same transaction.
/// </summary>
internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initialises a new instance of <see cref="EfUnitOfWork"/>.
    /// </summary>
    /// <param name="dbContext">The shared <see cref="ApplicationDbContext"/>.</param>
    public EfUnitOfWork(ApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
