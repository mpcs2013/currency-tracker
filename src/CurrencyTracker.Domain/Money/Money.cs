using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Exceptions;

namespace CurrencyTracker.Domain.Money;

/// <summary>
/// Combines a <see cref="decimal"/> amount with a
/// <see cref="CurrencyCode"/>. Supports addition and subtraction across
/// values in the same currency; throws
/// <see cref="CurrencyMismatchException"/> on currency mismatch.
/// Multiplication by a scalar is unconstrained.
/// </summary>
/// <param name="Amount">Numeric amount. Negative values are permitted
/// (debits, losses).</param>
/// <param name="Currency">ISO 4217 currency this amount is denominated in.</param>
public readonly record struct Money(decimal Amount, CurrencyCode Currency)
{
    /// <summary>Adds two <see cref="Money"/> values in the same currency.</summary>
    /// <exception cref="CurrencyMismatchException">If currencies differ.</exception>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new CurrencyMismatchException(left.Currency, right.Currency);
        }
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>Subtracts <paramref name="right"/> from <paramref name="left"/>.</summary>
    /// <exception cref="CurrencyMismatchException">If currencies differ.</exception>
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new CurrencyMismatchException(left.Currency, right.Currency);
        }
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>Scales <paramref name="left"/> by a scalar multiplier.</summary>
    public static Money operator *(Money left, decimal scalar) =>
        new(left.Amount * scalar, left.Currency);

    /// <inheritdoc/>
    public override string ToString() => $"{Amount} {Currency.Value}";
}
