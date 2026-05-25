using Wolverine.Http;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Trivial diagnostic query that returns <c>"pong"</c>. Exists to
/// exercise the Wolverine in-process dispatch path end-to-end without
/// touching any I/O. The first real CQRS query lands in Phase 9.
/// </summary>
public sealed record PingQuery();

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
