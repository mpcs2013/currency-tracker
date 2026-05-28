using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Exceptions;

namespace CurrencyTracker.Domain.Alerts;

/// <summary>
/// User-defined rule that fires an alert when the rate for a currency
/// pair changes by more than <see cref="ThresholdPercent"/> in absolute
/// terms. Owns the evaluation logic via <see cref="ShouldTrigger"/>.
/// </summary>
public sealed record AlertRule
{
    /// <summary>Gets the rule's identity.</summary>
    public Guid Id { get; }

    /// <summary>Gets the owning user's identifier.</summary>
    public Guid OwnerId { get; }

    /// <summary>Gets the base currency of the watched pair.</summary>
    public CurrencyCode Base { get; }

    /// <summary>Gets the quote currency of the watched pair.</summary>
    public CurrencyCode Quote { get; }

    /// <summary>Gets the absolute percent threshold (e.g. 1.5 means ±1.5%).</summary>
    public decimal ThresholdPercent { get; }

    /// <summary>Gets the delivery channel.</summary>
    public AlertChannel Channel { get; }

    /// <summary>Gets a value indicating whether the rule is currently active.</summary>
    public bool Enabled { get; private set; }

    private AlertRule(
        Guid id,
        Guid ownerId,
        CurrencyCode @base,
        CurrencyCode quote,
        decimal thresholdPercent,
        AlertChannel channel,
        bool enabled
    )
    {
        Id = id;
        OwnerId = ownerId;
        Base = @base;
        Quote = quote;
        ThresholdPercent = thresholdPercent;
        Channel = channel;
        Enabled = enabled;
    }

    /// <summary>Creates a validated <see cref="AlertRule"/> with a new identity.</summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="base">The base currency of the watched pair.</param>
    /// <param name="quote">The quote currency of the watched pair.</param>
    /// <param name="thresholdPercent">The absolute percent threshold (e.g. 1.5 means ±1.5%).</param>
    /// <param name="channel">The delivery channel.</param>
    /// <returns>A success carrying the entity, or a validation failure.</returns>
    public static Result<AlertRule> Create(
        Guid ownerId,
        CurrencyCode @base,
        CurrencyCode quote,
        decimal thresholdPercent,
        AlertChannel channel
    )
    {
        if (ownerId == Guid.Empty)
        {
            return Result<AlertRule>.Failure(
                DomainError.Validation("ALERT_OWNER_REQUIRED", "OwnerId is required.")
            );
        }

        if (@base == quote)
        {
            return Result<AlertRule>.Failure(
                DomainError.Validation(
                    "ALERT_SAME_CURRENCY",
                    "Base and quote currencies must differ."
                )
            );
        }

        if (thresholdPercent <= 0m)
        {
            return Result<AlertRule>.Failure(
                DomainError.Validation(
                    "ALERT_THRESHOLD_NONPOSITIVE",
                    "ThresholdPercent must be strictly positive."
                )
            );
        }

        if (thresholdPercent > 100m)
        {
            return Result<AlertRule>.Failure(
                DomainError.Validation(
                    "ALERT_THRESHOLD_RANGE",
                    "ThresholdPercent must be at most 100."
                )
            );
        }

        return Result<AlertRule>.Success(
            new AlertRule(
                Guid.NewGuid(),
                ownerId,
                @base,
                quote,
                thresholdPercent,
                channel,
                enabled: true
            )
        );
    }

    /// <summary>Enables the rule.</summary>
    public void Enable() => Enabled = true;

    /// <summary>Disables the rule.</summary>
    public void Disable() => Enabled = false;

    /// <summary>
    /// Returns <see langword="true"/> iff the rule should fire given the
    /// supplied rate transition. Returns <see langword="false"/> when
    /// disabled or when <paramref name="previousRate"/> is non-positive.
    /// </summary>
    /// <param name="previousRate">The earlier observed rate.</param>
    /// <param name="currentRate">The later observed rate.</param>
    public bool ShouldTrigger(decimal previousRate, decimal currentRate)
    {
        if (!Enabled)
        {
            return false;
        }

        if (previousRate <= 0m)
        {
            return false;
        }

        var changePercent = Math.Abs((currentRate - previousRate) / previousRate) * 100m;
        return changePercent >= ThresholdPercent;
    }

    /// <inheritdoc/>
    public bool Equals(AlertRule? other) => other is not null && Id == other.Id;

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();
}
