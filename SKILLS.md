# SKILLS.md — the `.claude/` setup

How agent behaviour is configured in this repo: what exists, when each piece
fires, why it exists, and — just as important — **what was deliberately not
built**.

This file is the **one-page index**. For per-artifact detail — full frontmatter,
what each body covers and refuses, the verification commands — see
[`docs/agents/reference.md`](docs/agents/reference.md). ADR
[0016](docs/decisions/0016-agent-configuration-in-claude-dir.md) is the decision
record.

## Document topology

Three files, one job each. Keeping them distinct is the whole point: this
project has twice paid for a convention living in two places.

```
CLAUDE.md    auto-loaded by Claude Code every session.
             @AGENTS.md import + a routing table. Nothing else.
                │
                ▼
AGENTS.md    canonical. Architecture contract, conventions, Don't list,
             quality gates, gotcha ledger. Read by every runtime —
             Claude Code, Codex, Cursor, Copilot.

SKILLS.md    this file. The .claude/ setup, for humans.

docs/workflow.md   the eight-step per-issue loop (human-facing).
docs/prompts.md    paste-ready prompts for runtimes with no invocation
                   mechanism. Retained, cross-linked to what supersedes
                   each one in Claude Code.
```

**The rule: never restate an `AGENTS.md` rule anywhere else.** `AGENTS.md` loads
into every session already. A second copy is not redundancy, it is a future
contradiction — `CLAUDE.md` documented a `/web` React frontend that has never
existed, and `AGENTS.md` claimed "Phase 0, no `.csproj` files yet" while seven
projects shipped underneath it.

## Which primitive for what

| | Fires when | Context | Use for |
| --- | --- | --- | --- |
| **Command** | You type `/name` | Main thread | Deliberate, argument-driven actions |
| **Skill** | Its `description` matches what you're doing | Main thread | Procedural knowledge `AGENTS.md` lacks |
| **Agent** | Dispatched by name | **Separate, fresh** | Read-heavy review that is self-contained |

The test for an **agent** is whether the task is high-token-in, low-token-out,
and answerable *from a fresh read*. If it needs the rationale the main thread
just produced, it must not be an agent — a subagent cannot see the conversation,
and splitting it off handicaps it on purpose.

The test for a **skill** is one question: **does it carry knowledge `AGENTS.md`
does not?** If the answer is no, it is a duplicate and will drift.

## The roster

### Commands — `.claude/commands/`

| `/gates` | Runs the quality gates in CI's order, stopping at the first failure. Encodes the ordering constraints (restore before `dotnet format`) and what *not* to do on failure (never suppress a CA rule, never weaken an architecture test). |
| --- | --- |
| `/adr <topic>` | Writes the next `docs/decisions/NNNN-*.md` in house structure. Numbering, the load-bearing `## Rejected` section, and the requirement that `## Consequences` records the loss. |
| `/drift` | Dispatches `docs-drift-auditor` and reports without editing. |

### Skills — `.claude/skills/<name>/SKILL.md`

| `ci-workflow-authoring` | The `.github/workflows/` house style: the `_reusable-` library rule, rationale header comments with caller lists, the permissions model, pinned action versions, and the requirement that `docs/ci-cd/pipelines.md` is updated in the same PR. Largest net-new surface — `AGENTS.md` has no CI-authoring content at all. |
| --- | --- |
| `terraform-module` | The `infra/terraform/` pattern: the three-file module shape, reasoning-carrying variable descriptions, the image treaty, the uat/prod envelope, OIDC-only identity, and which gates already run so you don't hand-audit checkov's job. |
| `wolverine-handler` | The Wolverine rules scattered across ADRs 0003/0006/0011/0012: convention discovery with no marker interfaces, the `AlwaysUseServiceLocationFor<T>` opt-in that must match across both hosts, the Postgres outbox, and business-key idempotency. |

### Agents — `.claude/agents/`

All three are read-only (`Read, Grep, Glob, Bash`; no `Edit`/`Write`).

| `docs-drift-auditor` | Checks the prose docs against what the repo contains. Its fresh context is the feature — it does not inherit the assumptions of the session that dispatched it. **Treats `docs/ci-cd/pipelines.md` §Recorded drifts as ratified** and never re-reports those rows; an auditor that cries wolf seven times per run gets ignored. |
| --- | --- |
| `azure-posture-reviewer` | `infra/**` and the deploy workflows, against the recorded treaty: no long-lived secrets (ADR 0014), environment-scoped OIDC subjects, switch-together envelopes, PROD-never-builds and digest promotion (ADR 0015), the one deliberate UAT→PROD `AcrPull` edge, and the image treaty. Explicitly does **not** re-run tflint/checkov/trivy, and defers app-code security to `/security-review`. |
| `observability-reviewer` | Spans, metrics, structured logs and redaction — the four questions, per operation, including when an operation should *not* be instrumented. |

## Deliberately not built

This section is load-bearing. Without it a future session helpfully re-adds
these, and each one is a second copy of something that already exists.

| Not built | Because |
| --- | --- |
| `testing-conventions` skill | 100% duplication. `AGENTS.md` already has §Testing conventions, §Test-writing rules and §Fakes live with tests. |
| `dotnet-quality-gates` skill | Its prose is `AGENTS.md` §Quality gates verbatim. The executable half became `/gates`. |
| `issue-workflow` skill | `docs/workflow.md` already holds the eight-step loop. A skill restating it is exactly the drift this setup exists to prevent. |
| `architecture-reviewer` agent | Its mechanical half is a **build gate** — `CurrencyTracker.Architecture.Tests` + NetArchTest check the dependency rule in IL on every `dotnet test`. Its judgment half ("is this ADR-worthy?", "is this abstraction speculative?") needs the conversation, which a subagent cannot see. |
| A general `security-reviewer` agent | Built-in `/security-review` already walks pending branch changes. What it cannot know is this repo's treaty, so the agent was narrowed to `azure-posture-reviewer`. |
| CA1848/CA1873 checks in `observability-reviewer` | They are compile errors under `TreatWarningsAsErrors` + `AnalysisMode=AllEnabledByDefault`. An agent reporting them reports something the build already refused. |
| Eleven mindset skills | `AGENTS.md` argued against this before the setup existed, and was right: six of the eleven only identified a directory. The taxonomy is now five rows, each routing to something real. |
| Anything frontend | Phase 16, unstarted, no code. The user-level `frontend-design` plugin covers it if it lands. |
| Duplicates of `Explore` / `Plan` / `general-purpose` | Built in. |

## Adding and retiring

Borrowed from `docs/prompts.md`, which had the right instinct:

**Add** when you have explained the same thing to an agent **three times across
different sessions** — and only after checking it isn't already in `AGENTS.md`.
Below three, it isn't reusable yet.

- One capability per artifact. A skill over ~150 lines is probably two.
- The `description` is what makes a skill fire — write it as trigger conditions
  ("Use whenever creating, editing or reviewing any file under
  `.github/workflows/`"), not as a summary.
- New agents and skills go on the protected-file list in `AGENTS.md` §Don't.

**Retire** when it hasn't fired in two phases. Delete it — don't archive, don't
comment it out. Git keeps the history; the roster should reflect current
practice. Then remove its row here and from the routing table in `CLAUDE.md`.

Frontmatter contract:

```yaml
# .claude/agents/<name>.md
---
name: kebab-case                    # matches the filename
description: What it does + when to use it.
tools: Read, Grep, Glob, Bash       # omit to inherit all; reviewers stay read-only
model: sonnet                       # optional
---

# .claude/skills/<name>/SKILL.md
---
name: kebab-case                    # matches the directory
description: Trigger conditions — this is what makes it fire.
---

# .claude/commands/<name>.md
---
description: One line, shown in the command list.
allowed-tools: Bash, Read, Edit     # optional
---
# $ARGUMENTS interpolates what the user typed
```

## What is committed, and what is not

`.gitignore` re-includes exactly three directories:

```
/.claude/*
!/.claude/agents/
!/.claude/skills/
!/.claude/commands/
```

Note `/.claude/*`, **not** `/.claude/`. Git does not descend into an ignored
*directory*, so a negation under a bare `/.claude` silently re-includes nothing.

**Machine-local, deliberately untracked:** `settings.json`,
`settings.local.json`, and the Visual Studio bridge scripts
(`vs-permission-hook.ps1`, `vs-debug-context-hook.ps1`, `vs-usage-hook.ps1`,
`vs-mcp-shim.ps1`). Those hooks invoke local scripts by relative path — commit
them and a fresh clone blocks every `Edit`/`Write` on a hook whose target does
not exist, with a 24-hour timeout.

### The csharpier hook — reapply this on a new machine

A `PostToolUse` hook formats every `.cs` file Claude touches, which turns the
`_reusable-format.yml` gate from something an agent must remember into something
that already happened. It is untracked as a consequence of the above, so the
block is recorded here verbatim. Add to `.claude/settings.json`:

```json
"PostToolUse": [
  {
    "hooks": [
      {
        "type": "command",
        "command": "powershell -NoProfile -ExecutionPolicy Bypass -File .claude/csharpier-format-hook.ps1",
        "timeout": 30
      }
    ],
    "matcher": "Edit|Write|MultiEdit"
  }
]
```

The script (`.claude/csharpier-format-hook.ps1`, also untracked) reads the hook
payload from stdin, exits early unless `tool_input.file_path` is an existing
`.cs` file inside the repo, then runs `dotnet csharpier format <file>` from the
repo root. It is fail-open — every path exits 0, because a formatter is never a
reason to block an edit that already succeeded.

Two things that will waste your time if you rewrite it:

- csharpier is a **local** tool (`.config/dotnet-tools.json`), so it is
  `dotnet csharpier`, and `format` is a **subcommand** — CSharpier 1.x has no
  `--write`/`--check` flags.
- csharpier honours `.gitignore`. Testing it on a file inside `.claude/` reports
  "Formatted 0 files" and looks like a broken hook.

## Related

- [`docs/agents/reference.md`](docs/agents/reference.md) — per-artifact detail: frontmatter, coverage, refusals, verification.
- [`AGENTS.md`](AGENTS.md) — canonical conventions; §Don't holds the protected-file list.
- [`CLAUDE.md`](CLAUDE.md) — the routing table.
- [`docs/workflow.md`](docs/workflow.md) — the eight-step per-issue loop.
- [`docs/prompts.md`](docs/prompts.md) — paste-ready prompts for other runtimes.
- ADR [0016](docs/decisions/0016-agent-configuration-in-claude-dir.md) — why this setup looks like this.
