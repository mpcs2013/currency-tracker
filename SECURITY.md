# Security Policy

We take security seriously. Thank you for helping keep CurrencyTracker
and its users safe.

## Reporting a vulnerability

**Do not open a public issue for a security vulnerability.** Public issues
are indexed immediately and visible to everyone watching the repo. Filing
a vulnerability as a public issue *is* a public disclosure.

Use GitHub's private advisory channel:

➡️ **[Report a vulnerability privately](https://github.com/mpcs2013/currency-tracker/security/advisories/new)**

This routes the report into a private fork only repo maintainers can see,
supports CVE assignment, and supports coordinated disclosure with a fix
ready before the issue goes public.

If you cannot use GitHub for any reason (no account, embargo, PGP-only
correspondence), email the maintainer at `security@your-domain.example`
with the subject line `CurrencyTracker security report — <one-line summary>`.
If you prefer PGP, request a key in a key-less first message; we will
respond out of band.

## What to include

- A clear description of the vulnerability and its impact.
- Steps to reproduce — the minimal reproducer, not a full chain.
- The version (commit SHA or release tag) you tested against.
- Any suggested fix or mitigation, if you have one.
- Whether you intend to publish a write-up after the fix, and on what
  timeline. We respect a disclosure deadline you set; we'll tell you if
  it's not achievable on our end.

## What you can expect

- **Acknowledgement within 72 hours.** Even on a weekend, you'll hear that
  the report was received.
- **Initial triage within 7 days.** Either a confirmation with an internal
  severity rating, or a request for more information, or a respectful
  decline with a reason.
- **Coordinated disclosure** once a fix is available. We credit reporters
  who want to be credited; we respect requests to remain anonymous.

## Supported versions

This project is in active development and not yet at `v1.0.0`. Only the
`main` branch is supported for security fixes today.

| Version       | Supported          |
| ------------- | ------------------ |
| `main`        | ✅ yes             |
| any tag       | ❌ pre-release     |

Once `v1.0.0` ships, this table will be updated to reflect the support
window (likely "latest minor + previous minor").

## Out of scope

- Vulnerabilities in third-party dependencies — report those upstream. We
  consume Dependabot bumps (configured in Phase 0.E) and triage them on
  receipt; if you've found a transitive vulnerability that hasn't been
  bumped, mention it in a regular issue, not via the security channel.
- Social-engineering attacks against the maintainer.
- Theoretical attacks with no demonstrated impact.