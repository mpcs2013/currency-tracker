using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace CurrencyTracker.ServiceDefaults.Logging;

/// <summary>
/// Redacts sensitive content from log-event properties before any sink
/// sees the event (Phase 11.12 forward rule, closed in 13.3). Two
/// passes over every string-valued scalar property: (1) properties
/// whose <em>name</em> matches the secret deny-list are replaced
/// wholesale; (2) remaining string values have email, JWT, and
/// Bearer-token matches replaced in place. Message-template text is
/// not touched — all production log sites are <c>[LoggerMessage]</c>
/// compile-time templates, so dynamic data can only enter through
/// properties, which makes property-level redaction complete for this
/// codebase.
/// </summary>
/// <remarks>
/// Known limitation: the <see cref="LogEvent.Exception"/> payload is
/// not rewritten. The Phase 11.12 audit confirmed no current code puts
/// tokens in exception messages; security reviews should re-verify
/// when new exception types are introduced.
/// </remarks>
public sealed partial class RedactingEnricher : ILogEventEnricher
{
    private const string Redacted = "[REDACTED]";

    private static readonly string[] SecretNameFragments =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "authorization",
        "credential",
    ];

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Snapshot: we mutate the collection while iterating.
        foreach (var (name, value) in logEvent.Properties.ToArray())
        {
            if (value is not ScalarValue { Value: string text })
            {
                continue; // non-string scalars and structured values pass through
            }

            if (NameLooksSecret(name))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name, Redacted));
                continue;
            }

            var scrubbed = BearerPattern().Replace(text, Redacted);
            scrubbed = JwtPattern().Replace(scrubbed, Redacted);
            scrubbed = EmailPattern().Replace(scrubbed, Redacted);

            if (!ReferenceEquals(scrubbed, text) && scrubbed != text)
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name, scrubbed));
            }
        }
    }

    private static bool NameLooksSecret(string propertyName) =>
        SecretNameFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)
        );

    // "Bearer <token>" — scheme plus the token that follows it. Runs
    // FIRST so the raw token can't be half-matched by the JWT pattern.
    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.None, 100)]
    private static partial Regex BearerPattern();

    // Three dot-separated base64url segments starting with the "eyJ"
    // JSON-object marker — the practical JWT shape.
    [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.None, 100)]
    private static partial Regex JwtPattern();

    // Pragmatic email shape — favour recall over RFC 5322 completeness.
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.None, 100)]
    private static partial Regex EmailPattern();
}
