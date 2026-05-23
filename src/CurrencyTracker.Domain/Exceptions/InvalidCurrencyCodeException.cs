namespace CurrencyTracker.Domain.Exceptions;

/// <summary>
/// Thrown when a currency code that should have been validated by
/// <c>CurrencyCode.Create</c> is constructed via a path that bypasses
/// the factory (deserialiser, reflection, EF Core materialisation
/// without the value-converter, etc.). The happy path is
/// <c>Result&lt;CurrencyCode&gt;.Failure</c>, not this exception.
/// </summary>
public sealed class InvalidCurrencyCodeException : DomainException
{
    /// <summary>
    /// Creates a new <see cref="InvalidCurrencyCodeException"/>.
    /// </summary>
    /// <param name="code">The offending input string.</param>
    public InvalidCurrencyCodeException(string code)
        : base($"'{code}' is not a valid ISO 4217 currency code.") { }
}
