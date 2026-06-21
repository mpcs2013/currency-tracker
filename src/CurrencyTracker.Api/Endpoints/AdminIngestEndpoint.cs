using CurrencyTracker.Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using Wolverine.Http;

namespace CurrencyTracker.Api.Endpoints;

/// <summary>
/// Dev-only HTTP trigger for manual rate ingestion. Dispatches
/// <see cref="IngestDailyRatesCommand"/> through the message bus and
/// returns <c>202 Accepted</c>. This is a developer convenience that
/// predates Phase 12's scheduled ingestion.
/// </summary>
/// <remarks>
/// SECURITY: this endpoint is intentionally UNAUTHENTICATED in Phase 9.
/// Authentication and authorisation land in Phase 11; until then, do not
/// expose this endpoint on a public ingress. It exists for local /
/// admin-triggered ingestion only.
/// </remarks>
public static class AdminIngestEndpoint
{
    /// <summary>
    /// Accepts an ingestion request and dispatches it to the handler.
    /// </summary>
    /// <param name="command">The ingestion command, bound from the
    /// request body. Validated by the FluentValidation middleware before
    /// the handler runs.</param>
    /// <param name="bus">The Wolverine message bus.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><c>202 Accepted</c> on success.</returns>
    [Authorize(Policy = "admin")]
    [WolverinePost("/admin/ingest")]
    public static async Task<IResult> Ingest(
        IngestDailyRatesCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        await bus.InvokeAsync(command, cancellationToken);

        return Results.Accepted();
    }
}
