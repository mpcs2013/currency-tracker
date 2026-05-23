using CurrencyTracker.Domain.Exceptions;

namespace CurrencyTracker.Domain.UnitTests.Exceptions;

/// <summary>
/// Tests covering the domain-exception hierarchy: message round-trip,
/// inner-exception propagation, and per-leaf message formatting.
/// </summary>
public sealed class DomainExceptionTests
{
    [Fact]
    public void DomainException_message_is_round_tripped()
    {
        var sut = new DomainException("boom");

        sut.Message.Should().Be("boom");
    }

    [Fact]
    public void DomainException_with_inner_exposes_inner()
    {
        var inner = new InvalidOperationException("inner");

        var sut = new DomainException("outer", inner);

        sut.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void InvalidCurrencyCodeException_message_includes_the_offending_code()
    {
        var sut = new InvalidCurrencyCodeException("USX");

        sut.Message.Should().Contain("USX");
    }

    [Fact]
    public void RateNotFoundException_message_includes_pair_and_date()
    {
        var asOf = new DateOnly(2026, 5, 21);

        var sut = new RateNotFoundException("USD", "EUR", asOf);

        sut.Message.Should().Contain("USD").And.Contain("EUR").And.Contain("2026-05-21");
    }

    [Fact]
    public void InvalidAlertRuleException_message_includes_the_reason()
    {
        var sut = new InvalidAlertRuleException("threshold must be positive");

        sut.Message.Should().Contain("threshold must be positive");
    }
}
