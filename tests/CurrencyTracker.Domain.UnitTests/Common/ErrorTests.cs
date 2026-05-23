using CurrencyTracker.Domain.Common;

namespace CurrencyTracker.Domain.UnitTests.Common;

/// <summary>
/// Tests covering the <see cref="DomainError"/> record's structural equality
/// and the <see cref="DomainError.Validation"/> convenience factory.
/// </summary>
public sealed class ErrorTests
{
    [Fact]
    public void Two_errors_with_the_same_fields_are_equal()
    {
        var a = new DomainError("INVALID_CODE", "Not a valid ISO 4217 code.");
        var b = new DomainError("INVALID_CODE", "Not a valid ISO 4217 code.");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Two_errors_with_different_codes_are_not_equal()
    {
        var a = new DomainError("INVALID_CODE", "msg");
        var b = new DomainError("MISSING_CODE", "msg");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Validation_factory_sets_code_and_message()
    {
        var sut = DomainError.Validation("X", "y");

        sut.Code.Should().Be("X");
        sut.Message.Should().Be("y");
    }
}
