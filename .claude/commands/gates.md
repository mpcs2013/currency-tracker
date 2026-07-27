---
description: Run the full quality-gate sequence locally, in the order CI runs it
allowed-tools: Bash, Read, Edit, Grep, Glob
---

Reproduce CI's verdict locally. Run these in order and **stop at the first
failure** — later gates assume earlier ones passed.

```
dotnet tool restore
dotnet csharpier check .
dotnet restore --locked-mode
dotnet format --verify-no-changes --no-restore
dotnet build -c Release
dotnet test -c Release --no-build
```

Notes that matter, each learned from a real failure:

- **csharpier is a local tool** (`.config/dotnet-tools.json`, pinned 1.2.6), so
  it is `dotnet csharpier`, and it is a `check` **subcommand** — `--check` is
  not a flag in CSharpier 1.x.
- **Restore before `dotnet format`.** It needs a restored project graph;
  `_reusable-format.yml`'s header records the ordering bug this comes from.
- **`dotnet build -c Release` treats warnings as errors** (`Directory.Build.props`,
  `AnalysisMode=AllEnabledByDefault`). An analyzer suggestion is a build failure.
- **`dotnet test` needs Docker running.** `Api.IntegrationTests`,
  `Infrastructure.IntegrationTests` and `AppHost.SmokeTests` use Testcontainers.
  If Docker is down, say so plainly rather than reporting the suite as failed.

## On failure

Fix the cause, don't route around the gate. Specifically:

- `csharpier check` failing → run `dotnet csharpier format .` and re-run.
- A CA-rule build error → fix the code. Do **not** add a suppression, edit
  `.editorconfig`, or lower `AnalysisMode`. Two rules bite most often here:
  CA1848 (production `ILogger` calls must use `[LoggerMessage]` source-generated
  methods on a `partial` class — see `src/CurrencyTracker.Api/ErrorHandling/`)
  and CA1873 (don't pass `exception.GetType().FullName` as a log parameter; pass
  the `Exception` itself).
- An architecture test failing → the dependency rule is the contract, not the
  test. Move the type, don't weaken the rule.

## Coverage

CI's floor is **58%**, applied by `_reusable-coverage-gate.yml` to the merged
Cobertura from all three test jobs. A single local `dotnet test` run does not
reproduce that merge, so don't report a local percentage as the gate's verdict.
Only run coverage locally if asked:

```
dotnet test -c Release --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

## Reporting

State each gate's real outcome. If you skipped one — Docker down, no network —
say which and why. Never report the sequence as green when a gate did not run.
