namespace CurrencyTracker.Domain.Common;

/// <summary>
/// Represents a domain-level failure in a structured form: a stable
/// machine-readable <see cref="Code"/> and a human-readable
/// <see cref="Message"/>. Errors are returned by <see cref="Result{T}"/>
/// from factory methods and never thrown; the corresponding thrown shape
/// is <see cref="CurrencyTracker.Domain.Exceptions.DomainException"/>.
/// </summary>
/// <param name="Code">Stable, machine-readable identifier of the failure
/// (e.g. <c>"INVALID_CURRENCY_CODE"</c>). Safe to log, safe to expose in
/// API responses, never localised.</param>
/// <param name="Message">Human-readable description of the failure.
/// Suitable for end-user display after localisation; not a stable contract.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Convenience factory for validation-shaped errors. Sets no fields
    /// beyond <see cref="Code"/> and <see cref="Message"/>; the
    /// "validation" categorisation is implicit in the error code's
    /// prefix convention.
    /// </summary>
    /// <param name="code">Stable error code.</param>
    /// <param name="message">Human-readable description.</param>
    /// <returns>A new <see cref="Error"/> with the supplied fields.</returns>
    public static Error Validation(string code, string message) => new(code, message);
}
