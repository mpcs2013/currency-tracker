using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Application.Abstractions.Security;
using CurrencyTracker.Application.Abstractions.Time;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Application.Ping;

/// <summary>
/// Hand-wired pseudo-handler that exercises four Phase 4 ports
/// together — clock, exchange-rate provider, unit of work, and
/// current user — before Phase 5 introduces Wolverine. The four
/// constructor parameters are deliberately many: Phase 5's Wolverine
/// rewrite makes the constructor disappear, which only feels like a
/// win if you've felt the cost first.
/// </summary>
public sealed class ManualPingHandler
{
    private readonly IDateTimeProvider _clock;
    private readonly IExchangeRateProvider _provider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _user;

    /// <summary>Constructs a handler with the four required ports.</summary>
    /// <param name="clock">Clock port.</param>
    /// <param name="provider">External rate provider port.</param>
    /// <param name="unitOfWork">Persistence commit port.</param>
    /// <param name="user">Identity port.</param>
    public ManualPingHandler(
        IDateTimeProvider clock,
        IExchangeRateProvider provider,
        IUnitOfWork unitOfWork,
        ICurrentUser user
    )
    {
        _clock = clock;
        _provider = provider;
        _unitOfWork = unitOfWork;
        _user = user;
    }

    /// <summary>
    /// Processes the ping: reads the clock, attempts a USD snapshot
    /// fetch for the clock's date, commits the unit of work, and
    /// returns a string carrying the user id and the date.
    /// </summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Token to cancel the
    /// orchestration at any await point.</param>
    /// <returns>On success: <c>Result.Success</c> with a string of the
    /// shape <c>"&lt;message&gt; @ &lt;date&gt; for &lt;userId&gt;"</c>.
    /// On failure: <c>PING_UNAUTHENTICATED</c> or the propagated
    /// provider failure.</returns>
    public async Task<Result<string>> HandleAsync(
        PingRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!_user.IsAuthenticated || _user.UserId is null)
        {
            return Result<string>.Failure(
                new DomainError("PING_UNAUTHENTICATED", "Anonymous callers cannot ping.")
            );
        }

        var asOf = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var usd = CurrencyCode.Create("USD").Value;

        var snapshotResult = await _provider
            .FetchAsync(usd, asOf, cancellationToken)
            .ConfigureAwait(false);
        if (snapshotResult.IsFailure)
        {
            return Result<string>.Failure(snapshotResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<string>.Success($"{request.Message} @ {asOf:yyyy-MM-dd} for {_user.UserId}");
    }
}
