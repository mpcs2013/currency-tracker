using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="ICurrencyRepository"/> fake backed by a
/// <see cref="Dictionary{TKey,TValue}"/> keyed on the currency code.
/// </summary>
public sealed class InMemoryCurrencyRepository : ICurrencyRepository
{
    private readonly Dictionary<CurrencyCode, Currency> _store = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Currency>>(_store.Values.ToList());
    }

    /// <inheritdoc />
    public Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.TryGetValue(code, out var currency) ? currency : null);
    }

    /// <inheritdoc />
    public Task AddAsync(Currency currency, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[currency.Code] = currency;
        return Task.CompletedTask;
    }
}
