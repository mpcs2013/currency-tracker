namespace CurrencyTracker.Domain.Exceptions;

/// <summary>
/// Thrown when an <c>AlertRule</c> is constructed in a state that
/// violates one of its invariants (e.g. <c>ThresholdPercent &lt;= 0</c>,
/// empty <c>OwnerId</c>) via a path that bypasses
/// <c>AlertRule.Create</c>. The happy path is
/// <c>Result&lt;AlertRule&gt;.Failure</c>.
/// </summary>
public sealed class InvalidAlertRuleException : DomainException
{
    /// <summary>
    /// Creates a new <see cref="InvalidAlertRuleException"/>.
    /// </summary>
    /// <param name="reason">Description of the violated invariant.</param>
    public InvalidAlertRuleException(string reason)
        : base($"AlertRule violates an invariant: {reason}") { }
}
