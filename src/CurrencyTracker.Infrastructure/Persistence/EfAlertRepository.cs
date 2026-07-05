using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Alerts;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IAlertRepository"/>. Stages writes on
/// the shared <see cref="ApplicationDbContext"/>; <c>IUnitOfWork</c>
/// commits.
/// </summary>
internal sealed class EfAlertRepository : IAlertRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfAlertRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Alert alert, CancellationToken cancellationToken) =>
        await _dbContext.Alerts.AddAsync(alert, cancellationToken);
}
