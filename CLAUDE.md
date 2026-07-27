# Currency Tracker — Claude Code entry point

@AGENTS.md

**`AGENTS.md`, imported above, is canonical.** It holds the architecture
contract, conventions, the `Don't` list, quality gates and the gotcha ledger,
and it is the file every runtime reads — Claude Code, Codex, Cursor, Copilot.
This file exists only because Claude Code auto-loads `CLAUDE.md`; it adds
routing and nothing else. **Never restate a rule from `AGENTS.md` here.** Two
copies of a convention is how this file ended up documenting a `/web` React
frontend that has never existed.

## Build & test

```
dotnet build -c Release                  # TreatWarningsAsErrors — a warning fails it
dotnet test  -c Release --no-build       # xUnit v3; needs Docker for Testcontainers
dotnet csharpier format .                # or --check, as CI runs it
dotnet format --verify-no-changes
dotnet run --project src/CurrencyTracker.AppHost   # Aspire orchestrates Postgres + Redis
```

`/gates` runs the whole sequence in CI's order.

## Routing

| When you're… | Reach for |
| --- | --- |
| Starting an issue | `docs/workflow.md` — the eight-step loop |
| Running the quality gates | `/gates` |
| Recording an architectural choice | `/adr <topic>` |
| Writing or changing `.github/workflows/**` | `ci-workflow-authoring` skill |
| Writing or changing `infra/terraform/**` | `terraform-module` skill |
| Touching a Wolverine handler, the outbox or scheduling | `wolverine-handler` skill |
| Reviewing a diff for spans / metrics / structured logs | `observability-reviewer` agent |
| Reviewing `infra/**` or a deploy workflow | `azure-posture-reviewer` agent |
| Reviewing app-code security | built-in `/security-review` |
| Suspecting the docs have drifted from reality | `/drift` |

`SKILLS.md` explains the full setup — what each artifact is, when it fires, and
what was deliberately *not* built.

## Two things worth knowing before you edit

- **The frontend does not exist.** No `/web`, no `package.json`. It is Phase 16
  and unstarted. Don't propose UI work, and don't reintroduce frontend
  conventions here — that was this file's stale content until 15.1.
- **Layer placement is enforced, not advised.** `CurrencyTracker.Architecture.Tests`
  checks the dependency rule in IL on every `dotnet test`. Don't reason about
  whether a reference is allowed; the build already answers.
