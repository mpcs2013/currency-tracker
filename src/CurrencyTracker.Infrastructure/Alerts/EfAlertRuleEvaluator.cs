using CurrencyTracker.Application.Abstractions.Alerts;
using CurrencyTracker.Application.Abstractions.Time;
using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTracker.Infrastructure.Alerts;

/// <summary>
/// EF Core adapter for <see cref="IAlertRuleEvaluator"/>. Loads the
/// <c>asOf</c> and <c>asOf - 1 day</c> snapshots plus the enabled rules
/// for the base, and delegates every decision to the domain:
/// <see cref="AlertRule.ShouldTrigger"/> for the threshold,
/// <see cref="Alert.Create"/> for the invariants.
/// </summary>
internal sealed class EfAlertRuleEvaluator : IAlertRuleEvaluator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    public EfAlertRuleEvaluator(ApplicationDbContext dbContext, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Alert>> EvaluateAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    )
    {
        var previousDay = asOf.AddDays(-1);

        var current = await _dbContext
            .RateSnapshots.AsNoTracking()
            .Include(s => s.Rates)
            .FirstOrDefaultAsync(s => s.Base == baseCurrency && s.AsOf == asOf, cancellationToken);
        var previous = await _dbContext
            .RateSnapshots.AsNoTracking()
            .Include(s => s.Rates)
            .FirstOrDefaultAsync(
                s => s.Base == baseCurrency && s.AsOf == previousDay,
                cancellationToken
            );

        // First-ingestion day (or a gap in the history): nothing to compare.
        if (current is null || previous is null)
        {
            return [];
        }

        var rules = await _dbContext
            .AlertRules.AsNoTracking()
            .Where(r => r.Base == baseCurrency && r.Enabled)
            .ToListAsync(cancellationToken);

        var fired = new List<Alert>();
        foreach (var rule in rules)
        {
            // A rule whose quote isn't in one of the snapshots can't be
            // evaluated today — skip it, don't fail the batch.
            if (
                !previous.TryGetRate(rule.Quote, out var previousRate)
                || !current.TryGetRate(rule.Quote, out var currentRate)
            )
            {
                continue;
            }

            if (!rule.ShouldTrigger(previousRate.Rate, currentRate.Rate))
            {
                continue;
            }

            var alert = Alert.Create(rule.Id, previousRate.Rate, currentRate.Rate, _clock.UtcNow);
            if (alert.IsSuccess)
            {
                fired.Add(alert.Value);
            }
        }

        return fired;
    }
}
