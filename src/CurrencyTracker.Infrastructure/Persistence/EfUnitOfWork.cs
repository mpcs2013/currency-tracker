using CurrencyTracker.Application.Abstractions.Persistence;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IUnitOfWork"/>. The full
/// implementation lands in 8.8; this placeholder exists so 8.4 can
/// wire the DI registrations and compile.
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
        throw new NotImplementedException("Implemented in 8.8.");
}
