using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="ICurrencyRepository"/>. Reads the
/// catalogue from <c>ApplicationDbContext.Currencies</c>; uses
/// &lt;see cref="DbSet{TEntity}.FindAsync"/&gt; for primary-key lookups so
/// the change tracker is consulted before the database.
/// </summary>
internal sealed class EfCurrencyRepository : ICurrencyRepository
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initialises a new instance of <see cref="EfCurrencyRepository"/>.
    /// </summary>
    /// <param name="dbContext">The shared <see cref="ApplicationDbContext"/>.</param>
    public EfCurrencyRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Currencies.ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Currency?> GetByCodeAsync(
        CurrencyCode code,
        CancellationToken cancellationToken
    ) => await _dbContext.Currencies.FindAsync([code], cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Currency currency, CancellationToken cancellationToken) =>
        await _dbContext.Currencies.AddAsync(currency, cancellationToken);
}
