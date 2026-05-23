namespace CurrencyTracker.Domain.Currencies;

/// <summary>
/// Read-side repository contract for looking up <see cref="Currency"/>
/// entities by identity or listing all supported currencies.
/// </summary>
public interface ICurrencyRepository
{
    /// <summary>
    /// Gets a currency by alphabetic ISO 4217 code.
    /// </summary>
    /// <param name="code">Currency identity code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching currency or <see langword="null"/> when absent.</returns>
    Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all supported currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All available currencies.</returns>
    Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken);
}
