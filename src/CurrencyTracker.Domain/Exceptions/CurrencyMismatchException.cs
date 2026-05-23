namespace CurrencyTracker.Domain.Exceptions;

/// <summary>
/// Thrown when an arithmetic operation on <c>Money</c> is attempted
/// across two different currencies (e.g. <c>moneyEur + moneyUsd</c>).
/// The operator's precondition cannot be expressed statically; the
/// caller is expected to check currency equality before invoking.
/// </summary>
public sealed class CurrencyMismatchException : DomainException
{
    /// <summary>
    /// Creates a new <see cref="CurrencyMismatchException"/>.
    /// </summary>
    /// <param name="left">Currency code of the left-hand operand.</param>
    /// <param name="right">Currency code of the right-hand operand.</param>
    public CurrencyMismatchException(string left, string right)
        : base($"Cannot combine Money values in different currencies: {left} and {right}.") { }
}
