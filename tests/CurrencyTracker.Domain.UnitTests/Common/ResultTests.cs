using CurrencyTracker.Domain.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CurrencyTracker.Domain.UnitTests.Common;

/// <summary>
/// Tests covering the <see cref="Result{T}"/> discriminated-union shape:
/// success/failure states, the <c>Match</c> projection, and the <c>Map</c>
/// transform.
/// </summary>
public sealed class ResultTests
{
    [Fact]
    public void Success_IsSuccess_is_true()
    {
        var sut = Result<int>.Success(42);

        sut.IsSuccess.Should().BeTrue();
        sut.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_IsFailure_is_true()
    {
        var sut = Result<int>.Failure(new Error("X", "boom"));

        sut.IsSuccess.Should().BeFalse();
        sut.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Match_calls_onSuccess_for_success_value()
    {
        var sut = Result<int>.Success(42);

        var actual = sut.Match(
            onSuccess: value => $"ok:{value}",
            onFailure: error => $"err:{error.Code}"
        );

        actual.Should().Be("ok:42");
    }

    [Fact]
    public void Match_calls_onFailure_for_failure_value()
    {
        var sut = Result<int>.Failure(new Error("X", "boom"));

        var actual = sut.Match(
            onSuccess: value => $"ok:{value}",
            onFailure: error => $"err:{error.Code}"
        );

        actual.Should().Be("err:X");
    }

    [Fact]
    public void Map_transforms_success_value()
    {
        var sut = Result<int>.Success(42);

        var actual = sut.Map(value => value.ToString());

        actual.IsSuccess.Should().BeTrue();
        actual.Match(s => s, _ => "").Should().Be("42");
    }

    [Fact]
    public void Map_preserves_failure()
    {
        var error = new Error("X", "boom");
        var sut = Result<int>.Failure(error);

        var actual = sut.Map(value => value.ToString());

        actual.IsFailure.Should().BeTrue();
        actual.Match(_ => new Error("Y", ""), e => e).Should().Be(error);
    }

    [Fact]
    public void Value_on_failure_throws_DomainException()
    {
        var sut = Result<int>.Failure(new Error("X", "boom"));

        var act = () => _ = sut.Value;

        act.Should().Throw<DomainException>().WithMessage("*Failure*");
    }

    [Fact]
    public void Error_on_success_throws_DomainException()
    {
        var sut = Result<int>.Success(42);

        var act = () => _ = sut.Error;

        act.Should().Throw<DomainException>().WithMessage("*Success*");
    }
}
