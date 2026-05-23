namespace CurrencyTracker.Domain.Exceptions;

/// <summary>
/// Base class for terminal domain failures — invariants violated by
/// code that bypassed the type's factory, conditions that cannot be
/// represented as <see cref="CurrencyTracker.Domain.Common.Result{T}"/>
/// failures (e.g. arithmetic operator preconditions), or domain-shaped
/// failures with no dedicated leaf class. Caught at the application
/// boundary by the global exception handler (Phase 6).
/// </summary>
public class DomainException : Exception
{
    /// <summary>Creates a new <see cref="DomainException"/> with the supplied message.</summary>
    /// <param name="message">Human-readable description of the failure.</param>
    public DomainException(string message)
        : base(message) { }

    /// <summary>
    /// Creates a new <see cref="DomainException"/> with the supplied message
    /// and inner exception.
    /// </summary>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <param name="innerException">The exception that triggered this one.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
