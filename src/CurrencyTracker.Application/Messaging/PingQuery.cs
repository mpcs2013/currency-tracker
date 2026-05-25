using FluentValidation;
using Wolverine.Http;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Trivial query used to prove the Wolverine dispatch pipeline. Carries
/// an optional <paramref name="Message"/> (default empty string) so the
/// Phase 6 validator has something to validate. Returns the string
/// <c>"pong"</c> via <see cref="PingHandler.Handle(PingQuery)"/>.
/// </summary>
/// <param name="Message">
/// Opaque message passed through to the handler. Validated by
/// <see cref="PingQueryValidator"/> — must be ≤ 100 characters.
/// Default empty string preserves the parameterless <c>new PingQuery()</c>
/// call site used by the unit test in Phase 5.6.
/// </param>
public sealed record PingQuery(string Message = "");

/// <summary>
/// Validator for <see cref="PingQuery"/>. Enforces a single rule:
/// <c>Message.Length &lt;= 100</c>. Discovered by Wolverine's
/// <c>opts.UseFluentValidation()</c> at host startup (Phase 6.3); not
/// instantiated directly by application code outside tests.
/// </summary>
public sealed class PingQueryValidator : AbstractValidator<PingQuery>
{
    /// <summary>
    /// Configures the validation rules. One rule today; the validator
    /// would gain more if <see cref="PingQuery"/> grew more fields,
    /// but the teaching point is the pipeline, not the rule.
    /// </summary>
    public PingQueryValidator()
    {
        RuleFor(x => x.Message)
            .MaximumLength(100)
            .WithMessage("Message must be 100 characters or fewer.");
    }
}

/// <summary>
/// Handler for <see cref="PingQuery"/>. Wolverine discovers this class
/// by convention: the class name ends in <c>Handler</c>, the
/// <c>Handle</c> method takes a known message type as its first
/// parameter, and both are <see langword="public"/>. No marker
/// interface is required — see
/// <c>docs/decisions/0003-wolverine-no-marker-interfaces.md</c>.
/// </summary>
public static class PingHandler
{
    /// <summary>
    /// Returns <c>"pong"</c> for every <see cref="PingQuery"/>. Sync
    /// because there's no I/O; the dispatcher accepts both sync and
    /// async handler signatures.
    /// </summary>
    /// <param name="query">The incoming query. The record has no
    /// payload so the parameter is unused; it's named to make the
    /// handler-to-message binding obvious to readers.</param>
    /// <returns>The literal string <c>"pong"</c>.</returns>
    [WolverineGet("/ping")]
    public static string Handle(PingQuery query) => "pong";
}
