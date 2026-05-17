<!--
PR template. Fill in every section. Empty sections are a smell — even "N/A"
is a deliberate answer. The reviewer (yourself today, a teammate tomorrow)
will scan this top to bottom; the discipline is to make their pass cheap.
-->

## What changed

<!-- One paragraph. What does this PR do, in user-facing or codebase-facing terms? -->

## Why

<!-- One paragraph. What problem does this solve? Why now? -->

Closes # <!-- issue number — REQUIRED, every PR closes an issue -->

---

## Self-review checklist

- [ ] **I can explain every line of this diff aloud, without notes.**
      (If you cannot, reject your own PR and rewrite the unclear block.)
- [ ] Tests were written **before** the implementation, or this PR is
      explicitly test-free (docs, config, infra-only). Tick this even on
      doc PRs — the box being deliberately checked is the signal.
- [ ] `dotnet build -c Release` is green locally.
- [ ] `dotnet test -c Release` is green locally (all tests, not just new ones).
- [ ] `dotnet csharpier check .` is silent.
- [ ] `dotnet format --verify-no-changes` is silent.
- [ ] README updated if the user-facing surface or quickstart changed.
- [ ] `CHANGELOG.md` updated if applicable (Phase 14 onwards).
- [ ] Conventional Commit format on the commit message (`feat(N.X):`,
      `fix(N.X):`, `chore(N.X):`, `docs(N.X):`, etc.).

## Build-matrix status checks

The PR cannot merge until all required status checks pass. The eight checks
configured by Phase 0.D are:

- [ ] `build (ubuntu-latest)`
- [ ] `build (windows-latest)`
- [ ] `test (ubuntu-latest)`
- [ ] `test (windows-latest)`
- [ ] `format`
- [ ] `codeql`
- [ ] `gitleaks`
- [ ] `dependency-review`

(Tick boxes once you have visually confirmed all eight are green on this PR.
If a check is unexpectedly missing, that's a CI configuration drift — open
a chore issue, do **not** merge.)

## Agent review

Run the Agents whose remit overlaps this PR. Tick `N/A` for the ones that
genuinely don't apply; never leave a row blank.

- [ ] **Domain Agent** — pure C# changes to `src/Domain/`.
- [ ] **Application Agent** — ports, handlers, CQRS messages.
- [ ] **Infrastructure Agent** — EF Core, Redis, HTTP clients, adapters.
- [ ] **API Agent** — HTTP surface, route conventions, ProblemDetails shape.
- [ ] **Worker Agent** — Wolverine, scheduled jobs, outbox.
- [ ] **Security Agent** — auth, secrets, JWT, RBAC, headers, rate limits.
- [ ] **Testing Agent** — test naming, fakes, integration vs unit boundary.
- [ ] **Observability Agent** — spans, metrics, structured logs, PII redaction.
- [ ] **Azure Agent** — Terraform, Container Apps, Key Vault, App Insights.
- [ ] **Documentation Agent** — README, ADRs, docs/ drift.
- [ ] **Frontend Agent** — React, TypeScript, Vitest, Playwright (Phase 16+).

## ADR gate

- [ ] This PR introduces an architectural decision → I have added an ADR
      under `docs/adr/NNNN-...` linked here: <!-- ADR-NNNN -->
- [ ] This PR does NOT introduce an architectural decision (most PRs).

An architectural decision is one that future PRs will lean on or be
constrained by. "We use EF Core" is an ADR; "I named this method `Foo`"
is not. When in doubt, write the ADR — it's cheap.

## Notes for the reviewer

<!-- Optional. Things you want explicit eyes on, alternative approaches
considered, links to design docs, things you nearly did but chose not to. -->