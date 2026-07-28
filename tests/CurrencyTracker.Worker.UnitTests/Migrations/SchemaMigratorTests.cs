using CurrencyTracker.Worker.Migrations;

namespace CurrencyTracker.Worker.UnitTests.Migrations;

/// <summary>
/// Tests for <see cref="SchemaMigrator.IsRequested"/>, the argument guard that
/// decides whether this process migrates and exits or starts as a normal
/// Worker. ADR 0018 requires the match to be exact: a near-miss must fall
/// through to the JasperFx command line and fail there, never silently start a
/// long-running host inside a Container Apps job that then burns its timeout.
/// </summary>
public sealed class SchemaMigratorTests
{
    [Fact]
    public void IsRequested_is_true_for_the_exact_argument()
    {
        var requested = SchemaMigrator.IsRequested(["--migrate"]);

        requested.Should().BeTrue();
    }

    [Fact]
    public void IsRequested_is_true_when_the_argument_appears_among_others()
    {
        var requested = SchemaMigrator.IsRequested(["--environment", "Production", "--migrate"]);

        requested.Should().BeTrue();
    }

    [Fact]
    public void IsRequested_is_false_for_no_arguments()
    {
        var requested = SchemaMigrator.IsRequested([]);

        requested.Should().BeFalse();
    }

    // Each of these is a plausible way to get the flag wrong in a Terraform
    // args list or in an `az containerapp job start` override. None may be
    // treated as a migration request.
    [Theory]
    [InlineData("--migrat")]
    [InlineData("--migrates")]
    [InlineData("migrate")]
    [InlineData("-migrate")]
    [InlineData("--Migrate")]
    [InlineData("--MIGRATE")]
    [InlineData("--migrate=true")]
    [InlineData(" --migrate")]
    public void IsRequested_is_false_for_a_near_miss(string argument)
    {
        var requested = SchemaMigrator.IsRequested([argument]);

        requested.Should().BeFalse();
    }
}
