using CurrencyTracker.Domain.Alerts;

namespace CurrencyTracker.Application.Abstractions.Persistence;

/// <summary>
/// Persistence port for <see cref="Alert"/> records. Deliberately tiny:
/// alerts are immutable facts, so the surface is "store one" and "load
/// one by id" (the dispatch stage's lookup). Queries over alerts are a
/// later phase's read-model concern.
/// </summary>
public interface IAlertRepository
{
    /// <summary>Loads the alert with the given identity, or <see langword="null"/>.</summary>
    /// <param name="id">The alert's identity.</param>
    /// <param name="cancellationToken">Token used to cancel the underlying I/O.</param>
    /// <returns>The alert, or <see langword="null"/> when no such alert exists.</returns>
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Stages <paramref name="alert"/> for insertion; the unit of work commits.</summary>
    /// <param name="alert">The alert to store.</param>
    /// <param name="cancellationToken">Token used to cancel the underlying I/O.</param>
    /// <returns>A task that completes when the alert is staged.</returns>
    Task AddAsync(Alert alert, CancellationToken cancellationToken);
}
