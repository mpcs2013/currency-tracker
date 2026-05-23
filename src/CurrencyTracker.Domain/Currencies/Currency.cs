using CurrencyTracker.Domain.Common;

namespace CurrencyTracker.Domain.Currencies;

/// <summary>
/// Currency entity identified by <see cref="Code"/>. Entity identity is
/// stable even if mutable display fields (for example <see cref="Name"/>)
/// change over time.
/// </summary>
public sealed record Currency
{
    /// <summary>Gets the ISO 4217 alphabetic currency code identity.</summary>
    public CurrencyCode Code { get; }

    /// <summary>Gets the human-readable currency name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the ISO 4217 numeric currency code.</summary>
    public int NumericCode { get; }

    private Currency(CurrencyCode code, string name, int numericCode)
    {
        Code = code;
        Name = name;
        NumericCode = numericCode;
    }

    /// <summary>
    /// Creates a <see cref="Currency"/> after validating the supplied
    /// display name and numeric code.
    /// </summary>
    /// <param name="code">Entity identity code.</param>
    /// <param name="name">Human-readable currency name.</param>
    /// <param name="numericCode">ISO 4217 numeric code in range 1..999.</param>
    /// <returns>A success carrying the entity, or a validation failure.</returns>
    public static Result<Currency> Create(CurrencyCode code, string name, int numericCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Currency>.Failure(
                DomainError.Validation("CURRENCY_NAME_REQUIRED", "A currency name is required.")
            );
        }

        if (numericCode is < 1 or > 999)
        {
            return Result<Currency>.Failure(
                DomainError.Validation(
                    "CURRENCY_NUMERIC_CODE_RANGE",
                    "A numeric code must be between 1 and 999."
                )
            );
        }

        return Result<Currency>.Success(new Currency(code, name, numericCode));
    }

    /// <summary>
    /// Renames this currency after validating the supplied display name.
    /// </summary>
    /// <param name="name">New human-readable currency name.</param>
    /// <returns>A success carrying this entity, or a validation failure.</returns>
    public Result<Currency> Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Currency>.Failure(
                DomainError.Validation("CURRENCY_NAME_REQUIRED", "A currency name is required.")
            );
        }

        Name = name;
        return Result<Currency>.Success(this);
    }

    /// <summary>Compares identity equality using <see cref="Code"/> only.</summary>
    /// <param name="other">Other currency to compare.</param>
    /// <returns><see langword="true"/> when codes match.</returns>
    public bool Equals(object? other) => other is not null && other is Currency && Code == other.Code;

    /// <summary>Returns a hash code derived from <see cref="Code"/> only.</summary>
    /// <returns>Identity hash code.</returns>
    public override int GetHashCode() => Code.GetHashCode();
}
