namespace CurrencyTracker.Domain.Alerts;

/// <summary>Delivery channel for a fired alert.</summary>
public enum AlertChannel
{
    /// <summary>Email delivery.</summary>
    Email = 0,

    /// <summary>Slack webhook delivery.</summary>
    Slack = 1,

    /// <summary>Generic HTTP webhook.</summary>
    Webhook = 2,
}
