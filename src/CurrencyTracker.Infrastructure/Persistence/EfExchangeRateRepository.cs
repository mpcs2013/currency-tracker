using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IExchangeRateRepository"/>.
/// Loads <see cref="RateSnapshot"/> aggregates with their owned
/// <see cref="ExchangeRate"/> collection eagerly via <c>Include</c>;
/// upsert is implemented by removing any existing snapshot for the
/// same <c>(Base, AsOf)</c> key and adding the supplied one.
/// </summary>
internal sealed class EfExchangeRateRepository : IExchangeRateRepository
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initialises a new instance of <see cref="EfExchangeRateRepository"/>.
    /// </summary>
    /// <param name="dbContext">The shared <see cref="ApplicationDbContext"/>.</param>
    public EfExchangeRateRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task<RateSnapshot?> GetLatestSnapshotAsync(
        CurrencyCode baseCurrency,
        CancellationToken cancellationToken
    ) =>
        _dbContext
            .RateSnapshots.Include(s => s.Rates)
            .Where(s => s.Base == baseCurrency)
            .OrderByDescending(s => s.AsOf)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<RateSnapshot?> GetSnapshotAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    ) =>
        _dbContext
            .RateSnapshots.Include(s => s.Rates)
            .FirstOrDefaultAsync(s => s.Base == baseCurrency && s.AsOf == asOf, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RateSnapshot>> GetSnapshotsInRangeAsync(
        CurrencyCode baseCurrency,
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext
            .RateSnapshots.Include(s => s.Rates)
            .Where(s => s.Base == baseCurrency && s.AsOf >= fromInclusive && s.AsOf <= toInclusive)
            .OrderBy(s => s.AsOf)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveSnapshotAsync(RateSnapshot snapshot, CancellationToken cancellationToken)
    {
        var existing = await _dbContext
            .RateSnapshots.Include(s => s.Rates)
            .FirstOrDefaultAsync(
                s => s.Base == snapshot.Base && s.AsOf == snapshot.AsOf,
                cancellationToken
            );

        if (existing is not null)
        {
            _dbContext.RateSnapshots.Remove(existing);
        }

        await _dbContext.RateSnapshots.AddAsync(snapshot, cancellationToken);
    }
}
