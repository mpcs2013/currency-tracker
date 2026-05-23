using CurrencyTracker.Domain.Common;

namespace CurrencyTracker.Domain.Currencies;

/// <summary>
/// ISO 4217 three-letter uppercase currency code (e.g. <c>USD</c>,
/// <c>EUR</c>). Validated against a static known-codes set on
/// construction via <see cref="Create"/>. The numeric ISO 4217 code is
/// not modelled here — it's a property of the <see cref="Currency"/>
/// entity (issue 3.3).
/// </summary>
public readonly record struct CurrencyCode
{
    private static readonly IReadOnlySet<string> KnownCodes = new HashSet<string>(
        StringComparer.Ordinal
    )
    {
        "USD",
        "EUR",
        "GBP",
        "JPY",
        "CHF",
        "CAD",
        "AUD",
        "NZD",
        "CNY",
        "INR",
        "BRL",
        "ZAR",
        "MXN",
        "SGD",
        "HKD",
    };

    /// <summary>Gets the three-letter uppercase ISO 4217 code.</summary>
    public string Value { get; }

    private CurrencyCode(string value) => Value = value;

    /// <summary>
    /// Validates the supplied raw input as an ISO 4217 alphabetic
    /// currency code and returns a <see cref="Result{CurrencyCode}"/>.
    /// Rejects null/empty, wrong-length, non-uppercase-letter, and
    /// unknown-code inputs with a stable error code per branch.
    /// </summary>
    /// <param name="raw">Candidate input — typically straight from a
    /// JSON deserialiser or query string.</param>
    /// <returns>Success carrying the parsed code, or failure with a
    /// <see cref="DomainError"/> identifying the rejected branch.</returns>
    public static Result<CurrencyCode> Create(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return Result<CurrencyCode>.Failure(
                DomainError.Validation("CURRENCY_CODE_REQUIRED", "A currency code is required.")
            );
        }

        if (raw.Length != 3)
        {
            return Result<CurrencyCode>.Failure(
                DomainError.Validation(
                    "CURRENCY_CODE_LENGTH",
                    "A currency code must be exactly three characters."
                )
            );
        }

        foreach (var ch in raw)
        {
            if (ch is < 'A' or > 'Z')
            {
                return Result<CurrencyCode>.Failure(
                    DomainError.Validation(
                        "CURRENCY_CODE_FORMAT",
                        "A currency code must contain only uppercase ASCII letters."
                    )
                );
            }
        }

        if (!KnownCodes.Contains(raw))
        {
            return Result<CurrencyCode>.Failure(
                DomainError.Validation(
                    "CURRENCY_CODE_UNKNOWN",
                    $"'{raw}' is not a currency this application supports."
                )
            );
        }

        return Result<CurrencyCode>.Success(new CurrencyCode(raw));
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
