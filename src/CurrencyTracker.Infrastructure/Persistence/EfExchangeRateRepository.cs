using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IExchangeRateRepository"/>. The full
/// implementation lands in 8.6; this placeholder exists so 8.4 can
/// wire the DI registrations and compile.
/// </summary>
internal sealed class EfExchangeRateRepository : IExchangeRateRepository
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initialises a new instance of <see cref="EfExchangeRateRepository"/>.
    /// </summary>
    /// <param name="dbContext">The shared <see cref="ApplicationDbContext"/>.</param>
    public EfExchangeRateRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public Task<RateSnapshot?> GetSnapshotAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException("Implemented in 8.6.");

    /// <inheritdoc />
    public Task SaveSnapshotAsync(RateSnapshot snapshot, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Implemented in 8.6.");
}
