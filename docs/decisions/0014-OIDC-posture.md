# 0014 — GitHub OIDC federation for Azure deploy: per-environment, no long-lived credentials

- **Status:** Accepted
- **Date:** 20.07.2026
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** 0001-stack-choices.md (chose Azure + Terraform + GitHub Actions
  with OIDC), 0010-oidc-jwt-auth.md (application-layer OIDC — a *distinct*
  concern from this deploy-layer one), Phase 13.C–13.D (the branch-protected
  `ci` gate every deploy PR passes first), Phase 14.A (the one-time manual
  bootstrap this ADR records), Phase 14.B (Terraform backend that consumes this
  state account), Phase 14.D (the deploy workflows that authenticate as these
  identities)

## Context

Phase 14 deploys the API and Worker to Azure from GitHub Actions. Something has
to authenticate the pipeline to Azure Resource Manager. The conventional path
stores a service-principal password as an `AZURE_CREDENTIALS` GitHub secret — a
long-lived credential that must be stored, rotated, and never leaked.

This ADR records the deploy-authentication posture established *manually* in
Phase 14.A, before any Terraform or workflow exists (identity and state must
precede the tool that would otherwise manage them — the bootstrap paradox). It
is deliberately separate from ADR 0010: 0010 governs how the *running API*
validates a caller's bearer token (request-time auth); this governs how the
*deploy pipeline* proves its identity to Azure (deploy-time auth). They share
the OIDC family but nothing else.

## Decision

- **One deploy identity per environment.** Two Entra ID App Registrations —
  `gh-deploy-uat`, `gh-deploy-prod` — each with a service principal and **no
  client secret or certificate**. Federation is their only authentication path.
- **Federated credential scoped to the GitHub Environment.** Subject
  `repo:mpcs2013/currency-tracker:environment:uat` (resp. `:prod`), issuer
  `https://token.actions.githubusercontent.com` (no trailing slash), audience
  `api://AzureADTokenExchange`. Exact string match, one credential per app.
- **Least-privilege scope.** Each identity holds `Contributor` on *its*
  environment resource group only — never subscription scope, never `Owner`.
  `Contributor` cannot create role assignments, so a leaked deploy token cannot
  escalate its own privileges.
- **State outlives what it manages.** Terraform remote state lives in
  `stcurrencytrackertfstate` / `tfstate` inside a dedicated
  `rg-currencytracker-tfstate` that no Terraform configuration references, so
  destroying an environment (14.58) never deletes the state describing it.
  Versioning + blob/container soft-delete guard the file.
- **Identifiers are variables; the gate fuses with the trust.**
  `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` are GitHub
  Environment *variables* (identifiers grant nothing without a matching token).
  The `prod` Environment's reviewer + wait timer + `main`/`v*` branch-tag
  restriction gate a job *before* GitHub mints a token with
  `sub = ...:environment:prod` — so PROD's Azure identity is reachable only
  through the gated path.
- **Deploy principals ≠ workload identities.** The data-plane roles the running
  app needs (AcrPull, Key Vault Secrets User, Postgres AAD admin, Redis Data
  Contributor — 14.24) are assigned to the Container Apps' *managed identities*,
  a separate identity set from these deploy principals. This ADR covers only the
  pipeline's identity.

## Considered and rejected

- **Stored service-principal secret (`AZURE_CREDENTIALS`).** The default in most
  tutorials. A long-lived, leakable credential requiring rotation — precisely
  what OIDC federation removes. Rejected.
- **One deploy identity for both environments.** Fewer objects, but a compromise
  of a UAT run could then act against PROD. Two identities keep the blast radius
  to one environment. Rejected.
- **Subscription-scope `Contributor` (or `Owner`).** Convenient — the pipeline
  never worries about scope again — and badly over-privileged: a poisoned deploy
  job reaches every resource group in the subscription. RG scope is the design.
  Rejected.
- **Branch-scoped subject (`ref:refs/heads/main`).** Trusts any job running on
  `main`, gated or not. Environment scope fuses the Azure trust with the same
  Environment that carries the reviewer gate — a strictly stronger guarantee.
  Rejected.
- **User-assigned managed identity instead of App Registrations.** Workable, but
  it adds an Azure resource that must be bootstrapped before Terraform and maps
  less cleanly onto per-GitHub-Environment federation. App Registrations keep
  the trust anchor outside the resource graph. Rejected for the bootstrap.
- **Azure flexible federation (claims-matching expressions / wildcards).**
  Preview-shaped complexity aimed at fleets of branches/repos. Two exact
  credentials cover a two-environment repo. Rejected.
- **Terraforming the state backend itself.** The bootstrap paradox: Terraform
  needs a backend before it can record having created one. The state account is
  definitionally manual and lives in an RG no config manages. Rejected.

## Consequences

- **No long-lived Azure credential exists anywhere** — no secret on the apps, no
  `AZURE_CREDENTIALS` in the repo. The Phase 13 `gitleaks` / `dependency-review`
  gate rides along unchanged; there is nothing for it to catch here.
- **Adding an environment is a documented, repeatable unit:** one App
  Registration + one federated credential + one RG + one GitHub Environment +
  one RG-scoped `Contributor` assignment. Nothing bespoke.
- **The manual bootstrap is auditable and reproducible** — every ID, scope, and
  gate is recorded in `docs/azure/bootstrap.md` (no secrets), which drives the
  clean-tenant reproduction in 14.59.
- **Backend auth is AAD.** The `tfstate` container is created with
  `--auth-mode login`, so `backend.tf` (14.10) must set `use_azuread_auth = true`
  (or `ARM_USE_AZUREAD=true`) rather than defaulting to an account key.
- The 20-federated-credential-per-app limit is irrelevant at two environments;
  revisit only if per-branch or per-PR credentials are ever introduced.

## Notes

- This ADR records a **posture, not a package** — it introduces no NuGet, npm,
  or Terraform-module dependency. (The phase's actual new top-level dependency,
  the Azure Monitor OpenTelemetry exporter at 14.48, gets its own ADR.)
- Terraform provider versions are pinned in `infra/terraform/versions.tf` (14.9),
  not in `Directory.Packages.props`; CPM governs .NET packages only.
- The resource providers registered during the 14.A pre-flight
  (`Microsoft.Storage`, `.KeyVault`, `.DBforPostgreSQL`, `.Cache`, `.App`,
  `.ContainerRegistry`, `.OperationalInsights`, `.Insights`, `.Network`) are a
  subscription-level prerequisite for the above and are listed in
  `docs/azure/bootstrap.md` for clean-tenant reproduction.