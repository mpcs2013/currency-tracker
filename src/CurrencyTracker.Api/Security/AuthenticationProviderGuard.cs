namespace CurrencyTracker.Api.Security;

/// <summary>
/// Boot-time assertion that the identity provider a deployment <i>declares</i>
/// agrees with the authority it was actually given.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately not a provider switch. Per ADR 0010 the Api is
/// IdP-agnostic: it validates whatever <c>Authentication:Authority</c> and
/// <c>Authentication:Audience</c> point at, and never names a provider in the
/// authentication pipeline. Branching on <c>Authentication:Provider</c> would
/// put knowledge of two specific identity providers back into the composition
/// root and make "config-only provider swap" false the day it was written down
/// as true.
/// </para>
/// <para>
/// The key still has to earn its keep, though: a declaration nothing reads is
/// worse than an absent one, because it looks authoritative. So it is asserted
/// rather than branched on. A stale <c>Authentication__Authority</c> against a
/// declared provider would otherwise boot green, pass <c>/health/live</c>, pass
/// <c>/health/ready</c> — neither touches auth — and then reject every token at
/// request time with a generic issuer-validation failure.
/// </para>
/// </remarks>
internal static class AuthenticationProviderGuard
{
    /// <summary>Declared-provider value meaning Microsoft Entra ID.</summary>
    private const string EntraId = "EntraId";

    /// <summary>
    /// The one property that distinguishes an Entra ID v2.0 issuer from a
    /// Keycloak realm, in every cloud. Matching on the host name
    /// (<c>login.microsoftonline.com</c>) would be narrower and would break in
    /// sovereign clouds for no benefit; every Keycloak authority ends in
    /// <c>/realms/&lt;name&gt;</c>.
    /// </summary>
    private const string EntraIssuerSuffix = "/v2.0";

    /// <summary>
    /// Throws when the declared provider and the configured authority disagree.
    /// </summary>
    /// <param name="provider">
    /// Value of <c>Authentication:Provider</c>, or <c>null</c> when the
    /// deployment declares nothing (local development, integration tests).
    /// </param>
    /// <param name="authority">The configured OIDC authority.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="provider"/> is <c>EntraId</c> and
    /// <paramref name="authority"/> is not an HTTPS Entra ID v2.0 issuer.
    /// </exception>
    public static void Validate(string? provider, string authority)
    {
        if (!string.Equals(provider, EntraId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var isEntraIssuer =
            Uri.TryCreate(authority, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.AbsolutePath.EndsWith(EntraIssuerSuffix, StringComparison.Ordinal);

        if (!isEntraIssuer)
        {
            throw new InvalidOperationException(
                $"Authentication:Provider is '{EntraId}', but Authentication:Authority "
                    + $"is '{authority}', which is not an HTTPS Microsoft Entra ID v2.0 "
                    + "issuer (expected a path ending in '/v2.0'). Either the deployment's "
                    + "Authentication__Authority environment variable is stale (see "
                    + "infra/terraform/envs/*/terraform.tfvars) or this host is running the "
                    + "wrong appsettings profile."
            );
        }
    }
}
