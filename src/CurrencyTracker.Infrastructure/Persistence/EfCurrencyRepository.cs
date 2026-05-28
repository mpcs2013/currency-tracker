using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="ICurrencyRepository"/>. The full
/// implementation lands in 8.7; this placeholder exists so 8.4 can
/// wire the DI registrations and compile.
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
    public Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException("Implemented in 8.7.");

    /// <inheritdoc />
    public Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Implemented in 8.7.");

    /// <inheritdoc />
    public Task AddAsync(Currency currency, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Implemented in 8.7.");
}
