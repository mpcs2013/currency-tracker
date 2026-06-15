# 0009 — Pin the PostgreSQL container image to an explicit major (18)

- **Status:** Accepted
- **Date:** <PR date>
- **Authors:** @your-handle
- **Supersedes:** —
- **Related:** 0001-stack-choices.md (Central Package Management / pin-the-stack),
  0004-ef-core-persistence.md (Postgres + Testcontainers), Phase 7.3 (the
  AppHost `postgres` resource + `currencytracker-pgdata` data volume)

## Context

The local-development PostgreSQL server is a container provided by
`Aspire.Hosting.PostgreSQL` (`builder.AddPostgres("postgres")`), with a named
data volume (`currencytracker-pgdata`) so dev data survives AppHost restarts.
Until now the *image major version* was implicit — whatever the installed
Aspire package defaulted to.

A security-driven bump of the Aspire packages to `13.4.3` silently moved that
default to PostgreSQL **18**. The official `postgres:18` image changed its
on-disk data layout (data now lives in a major-version subdirectory under
`/var/lib/postgresql`, to support `pg_upgrade --link`), and its entrypoint
refuses to start when it finds older-layout data in an existing volume. The
pre-existing `currencytracker-pgdata` volume — created by the previous major —
therefore caused the `postgres` resource to exit on startup. The data volume
was reset (`docker volume rm currencytracker-pgdata`) and the engine moved to
18.3.

The incident exposed the real problem: the database engine **major version was
being chosen by a transitive package default, not by us** — so a routine CVE
fix in Aspire changed the engine and broke local state. That is precisely the
class of silent upgrade ADR 0001 pins NuGet packages to avoid; the container
image deserves the same treatment.

## Decision

Pin the PostgreSQL container image to an explicit **major** tag, treating the
image as a pinned dependency on a par with the NuGet packages in
`Directory.Packages.props`:

- **AppHost** (`src/CurrencyTracker.AppHost/AppHost.cs`):

  ```csharp
  var postgres = builder
      .AddPostgres("postgres")
      .WithImage("postgres", "18")
      .WithDataVolume("currencytracker-pgdata");
  ```

- **Integration tests** (`Testcontainers.PostgreSql` fixture, Phase 10.8):

  ```csharp
  new PostgreSqlBuilder().WithImage("postgres:18") /* … */ .Build();
  ```

The bare-major tag (`18`) continues to track 18.x patch releases, so PostgreSQL
security fixes still flow in without a major jump. Dev, integration tests, and
(when it lands) the deployed database all run the same major.

## Considered and rejected

- **Leave the image major implicit (the Aspire default).** Rejected: this is
  what caused the incident — a CVE-driven Aspire bump silently moved the engine
  major and broke the persisted dev volume. Non-deterministic and surprising;
  the engine version should be a decision, not a side effect.
- **Pin to a full patch tag (e.g. `postgres:18.3`).** Rejected: it would freeze
  patch-level security fixes and require a manual bump for every CVE. The
  bare-major tag gets 18.x patches automatically while keeping the major fixed.
- **Stay on major 17.** Considered. The persisted dev data is disposable
  (rate snapshots, re-ingestable via `POST /admin/ingest`), so resetting the
  volume to move to 18 was cheap, and 18 is the current Aspire default — moving
  with it (deliberately, pinned) is the lower-friction forward choice.

## Consequences

- The PostgreSQL major is now an explicit, reviewed choice in two places
  (AppHost + the Testcontainers fixture), kept equal so tests exercise the
  engine that is shipped.
- A future Aspire bump cannot move the engine major as a side effect; moving to
  19 later is a deliberate change — bump the pin, reset or `pg_upgrade` the
  `currencytracker-pgdata` volume (the PG18+ single-mount layout applies), and
  update this ADR.
- The data volume now holds PG18-layout data; downgrading the pin below 18
  requires resetting the volume.
- An agent (or a drive-by edit) that changes the image tag "to get a newer
  Postgres" has changed this decision — that is an ADR update, not an
  incidental version bump.
