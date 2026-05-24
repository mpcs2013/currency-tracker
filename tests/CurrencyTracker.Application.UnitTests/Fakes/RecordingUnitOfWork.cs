using CurrencyTracker.Application.Abstractions.Persistence;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IUnitOfWork"/> fake that records the number of times
/// <see cref="SaveChangesAsync"/> was called. Tests assert on
/// <see cref="SaveCount"/> to confirm a handler did (or didn't) commit.
/// </summary>
public sealed class RecordingUnitOfWork : IUnitOfWork
{
    /// <summary>The number of times <see cref="SaveChangesAsync"/> has been called.</summary>
    public int SaveCount { get; private set; }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveCount++;
        return Task.FromResult(0);
    }
}
