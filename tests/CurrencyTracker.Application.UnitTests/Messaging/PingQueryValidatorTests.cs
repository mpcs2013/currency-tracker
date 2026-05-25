using CurrencyTracker.Application.Messaging;
using FluentValidation.Results;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Unit tests for <see cref="PingQueryValidator"/>. The validator's only
/// rule is <c>Message.Length &lt;= 100</c>; the two tests cover the
/// boundary directly. Both invoke <see cref="PingQuery"/>
/// without a Wolverine host — the validator is a pure function of its
/// input.
/// </summary>
public sealed class PingQueryValidatorTests
{
    [Fact]
    public void Message_at_100_chars_passes_validation()
    {
        // Arrange
        var validator = new PingQueryValidator();
        var query = new PingQuery(Message: new string('x', 100));

        // Act
        ValidationResult result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Message_at_101_chars_fails_validation_with_expected_property_name()
    {
        // Arrange
        var validator = new PingQueryValidator();
        var query = new PingQuery(Message: new string('x', 101));

        // Act
        ValidationResult result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].PropertyName.Should().Be("Message");
    }
}
