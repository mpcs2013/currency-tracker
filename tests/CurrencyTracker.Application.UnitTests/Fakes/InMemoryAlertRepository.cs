using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Alerts;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IAlertRepository"/> fake. Stored alerts are
/// visible via <see cref="Alerts"/>; lookups read the same list.
/// </summary>
public sealed class InMemoryAlertRepository : IAlertRepository
{
    private readonly List<Alert> _alerts = [];

    /// <summary>Gets the alerts added since this instance was created.</summary>
    public IReadOnlyList<Alert> Alerts => _alerts;

    /// <inheritdoc />
    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_alerts.FirstOrDefault(a => a.Id == id));
    }

    /// <inheritdoc />
    public Task AddAsync(Alert alert, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _alerts.Add(alert);
        return Task.CompletedTask;
    }
}
