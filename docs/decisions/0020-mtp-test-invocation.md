# 0020 — Run tests through Microsoft.Testing.Platform, and collect coverage with coverlet.MTP

- **Status:** Accepted
- **Date:** 21.09.2026
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** 0001-stack-choices.md (xUnit as the test framework), Phase 2.7
  (the test projects), Phase 13.14 (the measured coverage floor), Phase 14.28
  (the three-way split of the test work), `docs/ci-cd/pipelines.md`

## Context

Dependabot opened a bump of `xunit.v3` from 3.2.2 to 4.0.1 (PR #417). Every
test job failed, on a message that has nothing to do with any test:

```
error : Testing with VSTest target is no longer supported by
Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test,
you should opt-in to the new dotnet test experience.
```

xunit.v3 4.0 moves to **Microsoft.Testing.Platform 2.x** (visible in the
restored graph as `xunit.v3.mtp-v2`, `xunit.v3.core.mtp-v2`,
`Microsoft.Testing.Platform 2.4.0`), and MTP 2.x **removed the VSTest bridge on
the .NET 10 SDK**. An xUnit.net v3 test project has always been a self-executing
MTP application; until now `dotnet test` could still reach it through the
bridge. That route is gone, not deprecated.

This is therefore not a version bump that can be taken or declined on its
merits. Any future xunit.v3 4.x requires the migration, and the 3.x line will
not receive .NET 11-era fixes forever.

Two facts made the scope tractable:

- **The test code is unaffected.** `dotnet build -c Release` is clean at 0
  warnings, and with the opt-in alone all **292 tests across 9 projects pass**,
  Testcontainers suites included. There are no v2-to-v3 idioms left to purge and
  no API breaks in 4.0 that this codebase touches.
- **The current xunit.v3 3.2.2 already ships MTP 1.9.1**, above the 1.7 floor
  the MTP mode of `dotnet test` requires. The migration was verified against
  3.2.2 before the bump was taken, so the two changes are independent even
  though they land together.

What did break is CI, which collects coverage with VSTest-only flags:
`--collect:"XPlat Code Coverage" --settings coverlet.runsettings`. Under MTP
those are rejected with **exit code 5 and "Zero tests ran"** — a test-run
failure, not an obviously-a-flag failure.

## Decision

- **Opt into the MTP mode of `dotnet test` in `global.json`:**

  ```json
  "test": { "runner": "Microsoft.Testing.Platform" }
  ```

  One repo-wide switch, rather than `TestingPlatformDotnetTestSupport` in nine
  project files. The Microsoft guidance is explicit that a per-project setting
  risks a split solution where some projects run under VSTest and others under
  MTP, which is unsupported.

- **`coverlet.collector` becomes `coverlet.MTP`, at the same 10.0.1.** It is
  coverlet's own MTP extension and implements the collector's functionality —
  the same instrumentation engine at the same version, so measured coverage
  carries over and the 13.14 floor keeps its meaning. This is the whole reason
  it beat `Microsoft.Testing.Extensions.CodeCoverage`, which the xUnit docs
  recommend: a different engine would have produced a different percentage and
  forced a re-baseline of the floor in the same change that migrated the runner.
  Verified: merged line coverage is **89.4%** against a floor of 58%.

- **`coverlet.runsettings` becomes `testconfig.json`**, kept as a single file at
  the repo root and linked into every test project's output by
  `Directory.Build.targets`. coverlet.MTP reads it from the directory holding the
  test assembly, so it has to reach nine output folders; linking one file beats
  copying nine. The `[CurrencyTracker.*]*` include filter carries over verbatim —
  it is the one that keeps JasperFx/Wolverine's PDB-shipping assemblies from
  dragging the reported total from 86.6% to 33.4%.

- **`Directory.Build.targets`, not `Directory.Build.props`.** `.props` is
  imported before the project body, where `IsTestProject` is still empty and the
  condition would never fire. `.targets` is imported after
  `Microsoft.NET.Test.Sdk` has set it.

- **`Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` stay.** Both are
  dead weight under MTP, and xUnit's own guidance is to leave them until every
  development environment has moved off VSTest. Visual Studio test discovery is
  the concrete reason here; removing them is a separate, reversible cleanup.

## Consequences

- **`dotnet test <path.csproj>` no longer works — it is `--project <path>`.**
  The same applies to `--solution` and `--test-modules`. Both producing
  workflows were updated; anyone with the old form in a script or an IDE profile
  gets exit code 5.
- **Test-application arguments go after `--`.** `-- --coverlet` in CI. Options
  before the separator belong to `dotnet test`; options after are forwarded to
  the test app, which is where the coverlet extension lives.
- **Coverage filenames are timestamped** (`coverage.cobertura.<ddMMyyHHmmssfff>.xml`).
  That is what lets the seven projects in `_reusable-test.yml`'s loop share one
  results directory safely, and it is why the three workflows now glob
  `*cobertura*.xml`. A fixed filename would have silently matched nothing — the
  coverage gate's "verify artifacts arrived" step is what would have caught it.
- **A config file is authoritative to coverlet.MTP**, which injects no defaults
  when one is present. Every exclusion that used to arrive free from
  coverlet.collector is now written out in `testconfig.json`. Adding a filter
  there means adding it completely.
- **`UseMicrosoftTestingPlatformRunner` remains on only two of nine projects.**
  It is inconsistent and was so before this change; under xunit.v3 4.0 MTP is the
  only mode, so it no longer carries meaning. Left alone deliberately rather than
  churn nine files in a migration — worth a follow-up to remove or normalise.

## Alternatives considered

- **Hold the bump.** Pin xunit.v3 at 3.2.2 and tell Dependabot to ignore the 4.x
  major. Legitimate, and genuinely cheaper today — but it defers a migration that
  every future 4.x requires, and leaves the repo on a line that will age out.
- **`Microsoft.Testing.Extensions.CodeCoverage`.** The xUnit-documented path and
  more future-proof, but a different engine with a different runsettings schema
  and a different measured percentage. Re-baselining the coverage floor inside a
  runner migration would have made both changes unreviewable. Reconsider on its
  own merits later.
- **Keep VSTest by pinning `Microsoft.Testing.Platform` below 2.0.** Fights a
  transitive of xunit.v3 4.0 and re-breaks on the next bump. Not a fix.
