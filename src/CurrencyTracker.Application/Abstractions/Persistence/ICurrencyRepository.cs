using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Application.Abstractions.Persistence;

/// <summary>
/// Persistence port for <see cref="Currency"/> entities. The Phase 8
/// adapter <c>EfCurrencyRepository</c> implements this against EF Core.
/// </summary>
public interface ICurrencyRepository
{
    /// <summary>
    /// Gets all supported currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All available currencies.</returns>
    Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the <see cref="Currency"/> identified by the supplied
    /// <see cref="CurrencyCode"/>, or <see langword="null"/> if no
    /// currency with that code exists.
    /// </summary>
    /// <param name="code">ISO 4217 alphabetic code identifying the currency.</param>
    /// <param name="cancellationToken">Token to cancel the underlying I/O.</param>
    /// <returns>The currency, or <see langword="null"/> if not found.</returns>
    Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken);

    /// <summary>
    /// Tracks <paramref name="currency"/> for insertion on the next
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> call.
    /// </summary>
    /// <param name="currency">The currency to add.</param>
    /// <param name="cancellationToken">Token to cancel the underlying I/O.</param>
    Task AddAsync(Currency currency, CancellationToken cancellationToken);
}
