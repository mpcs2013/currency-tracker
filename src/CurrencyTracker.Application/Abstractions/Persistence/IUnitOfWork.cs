namespace CurrencyTracker.Application.Abstractions.Persistence;

/// <summary>
/// Persists outstanding changes tracked by the active
/// <see cref="ICurrencyRepository"/> / <see cref="IExchangeRateRepository"/>
/// instances atomically. The Phase 8 adapter <c>EfUnitOfWork</c>
/// delegates to <c>DbContext.SaveChangesAsync</c>; the implicit
/// transaction EF Core opens around <c>SaveChangesAsync</c> is the
/// transaction boundary.
/// </summary>
/// <remarks>
/// The interface deliberately omits explicit transaction-control
/// methods (<c>BeginTransactionAsync</c>, <c>CommitAsync</c>,
/// <c>RollbackAsync</c>). Application code that needs distributed
/// transactional behaviour will reach for Phase 12's Wolverine
/// outbox, not for this interface.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Atomically persists all tracked changes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the underlying
    /// I/O. Cancellation between tracking and saving leaves the
    /// changes un-persisted, as expected.</param>
    /// <returns>The number of state entries affected.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
