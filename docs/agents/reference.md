# Claude Code setup — detailed reference

Per-artifact documentation for everything under `.claude/`: full frontmatter,
what each body covers, what it deliberately refuses, and how to maintain the set.

This is the **deep reference**. [`SKILLS.md`](../../SKILLS.md) is the one-page
index — read that first if you just need to know which artifact to reach for.
ADR [0016](../decisions/0016-agent-configuration-in-claude-dir.md) is the
decision record for why the setup looks like this.

---

## 1. Document topology

Five files, five altitudes. The rule that keeps them from becoming five copies of
each other: **never restate an `AGENTS.md` rule anywhere else.**

| File | Lines | Loaded | Job |
| --- | --: | --- | --- |
| `CLAUDE.md` | 50 | Auto, every session | `@AGENTS.md` import + routing table. Nothing else. |
| `AGENTS.md` | 411 | Via the import | **Canonical.** Architecture contract, conventions, Don't list, quality gates, gotcha ledger. Read by every runtime — Claude Code, Codex, Cursor, Copilot. |
| `SKILLS.md` | 206 | On request | The `.claude/` roster, one page, for humans. |
| `docs/agents/reference.md` | this file | On request | Per-artifact detail. |
| `docs/workflow.md` | 92 | On request | The eight-step per-issue loop. |
| `docs/prompts.md` | 241 | On request | Paste-ready prompts for runtimes with no invocation mechanism. |

The import direction is deliberate. Claude Code auto-loads `CLAUDE.md`; every
other runtime reads `AGENTS.md`. Only one file can be canonical, and it is
`AGENTS.md` — so `CLAUDE.md` imports it and adds routing only.

Why this matters here specifically: the project has already paid for the
alternative twice. `CLAUDE.md` documented a `/web` React frontend that has never
existed, and `AGENTS.md` claimed *"Phase 0 — no `.csproj` files exist yet"* while
seven projects shipped underneath it.

---

## 2. Choosing a primitive

|  | Fires when | Context | Right for |
| --- | --- | --- | --- |
| **Command** | You type `/name` | Main thread | Deliberate, argument-driven actions |
| **Skill** | Its `description` matches the work | Main thread | Procedural knowledge `AGENTS.md` lacks |
| **Agent** | Dispatched by name | **Separate, fresh** | Read-heavy review answerable from a cold read |

**The agent test.** Is the task high-token-in, low-token-out, and answerable
*from a fresh read*? If it needs the design rationale the main thread just
produced, it must not be an agent — a subagent cannot see the conversation, so
splitting it off handicaps it on purpose. This is why there is no
`architecture-reviewer`.

**The skill test.** One question: does it carry knowledge `AGENTS.md` does not?
`AGENTS.md` loads into every session already. A skill that restates it is not
redundancy, it is a future contradiction.

**The command test.** Is it invoked deliberately, ideally with an argument? Then
it is a command, even if it also carries procedure.

---

## 3. Commands — `.claude/commands/`

Invoked by typing `/name`. `$ARGUMENTS` interpolates what follows.

### `/gates` — 59 lines

```yaml
---
description: Run the full quality-gate sequence locally, in the order CI runs it
allowed-tools: Bash, Read, Edit, Grep, Glob
---
```

**Covers.** The six-command sequence, in CI's order, stopping at the first
failure:

```
dotnet tool restore
dotnet csharpier check .
dotnet restore --locked-mode
dotnet format --verify-no-changes --no-restore
dotnet build -c Release
dotnet test -c Release --no-build
```

Plus the constraints that make the order non-arbitrary:

- csharpier is a **local** tool (`.config/dotnet-tools.json`, pinned 1.2.6) → it
  is `dotnet csharpier`, and `check` is a **subcommand**. CSharpier 1.x has no
  `--check` flag. `AGENTS.md` carried the wrong form until 15.1.
- `dotnet format` needs a restored project graph, so restore precedes it.
  `_reusable-format.yml`'s header records the bug that taught the project this.
- `dotnet build -c Release` treats warnings as errors — an analyzer suggestion
  is a build failure.
- `dotnet test` needs Docker for the three Testcontainers suites.

**On failure it refuses the easy route.** No CA-rule suppression, no
`.editorconfig` edit, no lowering `AnalysisMode`, no weakening an architecture
test. It names the two rules that bite most often (CA1848 `[LoggerMessage]`,
CA1873 exception-type-as-parameter) and points at
`src/CurrencyTracker.Api/ErrorHandling/` for the pattern.

**Defers to.** CI for the real coverage verdict — the 58% floor is applied by
`_reusable-coverage-gate.yml` to merged Cobertura from three separate test jobs,
which a single local run cannot reproduce. The command says so rather than
reporting a misleading local number.

**Reports honestly.** If Docker is down or a gate was skipped, it says which and
why instead of calling the sequence green.

---

### `/adr <topic>` — 69 lines

```yaml
---
description: Write the next architecture decision record in docs/decisions/
allowed-tools: Bash, Read, Write, Grep, Glob
---
```

**Covers.** Numbering (scan `docs/decisions/`, increment, zero-pad to four),
lowercase-kebab filenames, and the house structure:

```markdown
# NNNN — <the decision, not the topic>

Date: YYYY-MM-DD · Status: accepted · Phase: <N.N> (issue <N>)

## Decision
## Rejected
## Consequences
## Related
```

**The three rules that make these good**, all drawn from reading the existing 15:

1. The **title states the decision**, not the subject area — "Deploy topology:
   single-revision health-gated cutover, digest promotion", not "Deployment".
2. **`## Rejected` is load-bearing.** It is what stops a future session
   helpfully reintroducing the thing. Each entry names the alternative in bold
   and gives the *mechanism* that killed it, not a preference.
3. **`## Consequences` records the loss.** ADR 0015 ends *"what is genuinely
   lost is manual canarying"*. An ADR with only upsides is not finished.

**Knows the exception.** `0014-OIDC-posture.md` breaks the lowercase-kebab
convention. The command flags it as the exception rather than propagating it,
and does not rename it — inbound links in ADR 0015 and `pipelines.md` depend on
the current name.

**Then.** Reminds that the ADR and the code it justifies ship in the **same PR**
(step 7 of `docs/workflow.md`), and that adding a rule to `AGENTS.md` needs the
issue to name `AGENTS.md` as a deliverable.

---

### `/drift` — 23 lines

```yaml
---
description: Audit the project docs for drift against what the repo actually contains
allowed-tools: Agent, Bash, Read, Grep, Glob
---
```

**Covers.** Dispatching `docs-drift-auditor` with a scope and **nothing else**.

The instruction that matters: *do not summarise this conversation into the
prompt.* The agent's value is that it checks docs against the repo rather than
against whatever the session has been assuming for the last hour. Passing it
context destroys the thing you dispatched it for.

**Refuses to fix.** Most files in scope are on the protected-file list in
`AGENTS.md` §Don't and need an issue naming them as a deliverable. It reports,
then asks.

---

## 4. Skills — `.claude/skills/<name>/SKILL.md`

Fire automatically when the `description` matches the work. Write descriptions
as **trigger conditions**, not summaries — that field is the firing mechanism.

### `ci-workflow-authoring` — 164 lines

```yaml
---
name: ci-workflow-authoring
description: House style for authoring or changing GitHub Actions workflows in .github/workflows/ — the _reusable- library convention, rationale header comments, permissions model, pinned action versions, and the rule that pipelines.md is updated in the same PR. Use whenever creating, editing, reviewing or debugging any file under .github/workflows/.
---
```

Largest net-new surface in the roster: `AGENTS.md` has **zero** CI-authoring
content, while `.github/workflows/` holds 17 files with a strikingly regular
style — 10 `_reusable-*.yml` libraries and 7 leaves.

**Covers.**

- **The library rule.** `_reusable-` means `workflow_call` **only**. A reusable
  with zero callers gets **deleted** — `_reusable-build.yml` was removed for
  exactly this (recorded drift 14.28/14.35).
- **The rationale header.** Every file opens with reasoning, and reusables list
  their callers. This is the most distinctive thing about the repo's workflows,
  and the headers carry load-bearing archaeology. Change a file → update its
  header. Add a caller → update the `Callers:` line.
- **The skeleton** — `description` on every `workflow_call` input,
  `permissions: contents: read` at workflow level with per-job escalation,
  `timeout-minutes` on every job, `name:` matching the filename, and
  `cancel-in-progress` gated on `github.event_name == 'pull_request'` so trunk
  and deploy runs are never cancelled.
- **Pinned versions — check, don't remember.** Current: `checkout@v7`,
  `setup-dotnet@v6`, `cache@v6`, `upload/download-artifact@v7`. The skill tells
  you to grep rather than trust the table, because `AGENTS.md` was wrong about
  `setup-dotnet` for thirteen phases.
- **Deploy invariants** — the switch-together rule, OIDC-only, the image treaty,
  single responsibility per stage (`_reusable-docker-build.yml` deliberately
  cannot push; a `push` input would let a caller skip the Trivy gate), and
  PROD-never-builds.
- **Coverage producers.** Upload as `coverage-raw-*` and the gate picks it up by
  pattern — following the naming convention is the entire integration.

**The closing rule.** `docs/ci-cd/pipelines.md` is derived from these files and
rots the moment one changes without it. Same PR: update the graph, and add a row
to §Recorded drifts if the change departs from the phase plan.

---

### `terraform-module` — 134 lines

```yaml
---
name: terraform-module
description: House pattern for the Azure Terraform under infra/terraform/ — the main/variables/outputs module triple, the uat/prod envelope split, OIDC-only identity, the container-image treaty, and the gates that run on every plan. Use whenever creating, editing or reviewing any .tf/.hcl file, or wiring a new Azure resource.
---
```

**Covers.**

- **Module shape** — exactly `main.tf` / `variables.tf` / `outputs.tf`. No
  `providers.tf` inside a module. `main.tf` opens with a comment stating what
  the module owns *and what it deliberately does not*.
- **Variable descriptions carry reasoning.** The repo's strongest Terraform
  habit: a description says why the default is what it is, what it couples to,
  and when to change it. A one-word description is a defect. The skill quotes
  `use_acr_registry` as the exemplar.
- **The image treaty** —
  `ignore_changes = [template[0].container[0].image]`. Terraform owns
  infrastructure shape; the pipeline owns the image. Never widen it to make a
  plan clean; a second exemption is ADR-worthy. Notes that `postgres`'s
  `ignore_changes` on `zone` is a *different*, documented rationale (Azure moves
  them on failover) — same rule, don't confuse them.
- **Identity** — system-assigned managed identities, Key Vault references,
  `ARM_USE_OIDC`, and the single deliberate cross-environment edge: PROD's
  read-only `AcrPull` on the **UAT** ACR for digest promotion.
- **The environment envelope** — `envs/{uat,prod}/{backend.hcl,terraform.tfvars}`
  selected together by one input. A new environment-varying value goes in
  **both** tfvars files in the same PR.

**Defers to.** `tflint` and `checkov`, which already run in
`_reusable-terraform.yml`. The skill says explicitly: don't hand-audit what they
cover, and don't silence them — fix or add a documented exception.

**Don'ts with teeth.** Don't delete `.terraform.lock.hcl` or `init -upgrade` to
clear an error; don't commit state; don't apply from a workstation against PROD.

---

### `wolverine-handler` — 132 lines

```yaml
---
name: wolverine-handler
description: Wolverine conventions and footguns for this codebase — convention-based handler discovery with no marker interfaces, the AlwaysUseServiceLocationFor codegen opt-in that must match across both hosts, the Postgres outbox/inbox, and business-key idempotency. Use whenever adding or changing a message, a Wolverine handler, the outbox, scheduling, or either host's UseWolverine block.
---
```

Consolidates rules currently scattered across `AGENTS.md` Don'ts, Gotchas, and
four ADRs. The two `*wolverine-describe*.txt` dumps in `docs/` are physical
evidence of repeated debugging here.

**Covers.**

- **No marker interfaces (ADR 0003).** Discovery scans for public `*Handler`
  types and binds on the `Handle`/`Consume` **parameter type**. No
  `IRequest<T>`, `ICommand`, `IMessage`. The MediatR shape dominates training
  data, so the instinct will surface — reject it every time.
- **The `AlwaysUseServiceLocationFor<T>` footgun (ADR 0006)** — the most
  expensive recurring failure. Adapters are `internal sealed` by the cross-layer
  guardrails; Wolverine 6 prefers to inline-construct, which needs a *public*
  concrete; `ServiceLocationPolicy.NotAllowed` forbids the fallback and throws
  `InvalidServiceLocationException` **at host startup**.

  The skill carries a table of both hosts' current opt-ins and flags that they
  are **asymmetric** — the Worker declares `IAlertRuleEvaluator` and
  `IAlertRepository`, the Api does not. So it says: add to both in the same PR
  unless you have positively established otherwise, and don't infer the rule
  from the current state. Failure is at startup, so `AppHost.SmokeTests` is what
  catches it — run the full suite.
- **Outbox and transactions (ADR 0011)** — durable inbox/outbox in the existing
  Postgres under a `wolverine` schema; `AutoApplyTransactions()` means **no
  handler needs `[Transactional]`**; don't publish through an injected bus.
- **Idempotency is a business key (ADR 0012).** The inbox dedupes by *envelope
  id* and cannot know two different envelopes describe the same fact. Both
  layers required: the polite layer (a pre-filtering query) and the guarantee
  layer (a UNIQUE index on the business identity).

**Ships a checklist** covering message shape, the opt-in, `CancellationToken`
with no default, `[LoggerMessage]`, the business key, fakes-over-mocks, and the
full test run.

---

## 5. Agents — `.claude/agents/`

All three are read-only — `tools: Read, Grep, Glob, Bash`, no `Edit`/`Write`,
`model: sonnet`. They run in a **separate context** and cannot see the
conversation that dispatched them.

### `docs-drift-auditor` — 88 lines

```yaml
---
name: docs-drift-auditor
description: Audits the project's prose docs (AGENTS.md, CLAUDE.md, SKILLS.md, README.md, docs/ci-cd/pipelines.md) against what the repository actually contains, and reports concrete drift. Use when docs may have gone stale, before trusting AGENTS.md, or via /drift.
tools: Read, Grep, Glob, Bash
model: sonnet
---
```

Strongest agent in the roster, and the acceptance test for the 15.1 de-staling
work. Its fresh context is the **feature**: an auditor that sat through the
session inherits the session's assumptions.

**The rule that keeps it useful.** `docs/ci-cd/pipelines.md` §Recorded drifts is
a **ratified ledger** of deliberate plan-vs-implementation divergences. The
agent reads it first and never reports a ledger row as a finding. An auditor
that re-reports seven known drifts every run gets ignored by the third run. If a
ledger row itself has gone stale, it says so explicitly and quotes both.

**Method** — a verification table pairing claim types with cheap checks, each
drawn from drift that actually happened here:

| Claim | Check |
| --- | --- |
| "Current phase is N" | `gh issue list --state open`, milestones, `git log` |
| A directory exists | `ls` / `Glob` — `CLAUDE.md` documented a `/web` that never existed |
| Action versions | `grep -rn "uses:" .github/workflows/` |
| Counts | `ls` and count |
| Tool syntax | Run `--help` — `csharpier --check` was documented for phases |
| "Lands in phase N" | Future tense that already shipped is drift |

**Explicitly not findings.** Ledger rows; phrasing preferences; clearly-marked
future plans; missing documentation (absence is not drift).

**Output.** Verdict first — **CLEAN** or **N findings** — then per finding:
claim quoted, evidence with the command that shows it, severity
(high = following it breaks a build or deploy), and a ready-to-apply fix. It is
told never to pad a clean run.

---

### `azure-posture-reviewer` — 118 lines

```yaml
---
name: azure-posture-reviewer
description: Reviews infra/terraform/** and the deploy workflows against this project's recorded Azure treaty — the no-long-lived-secrets OIDC posture (ADR 0014), digest-not-tag promotion and the single cross-environment AcrPull edge (ADR 0015), and the container-image ignore_changes treaty. Use before merging IaC or deploy-pipeline changes. Not for application-code security — that is /security-review.
tools: Read, Grep, Glob, Bash
model: sonnet
---
```

**Its niche, stated before anything else.** `tflint`, `checkov`, `trivy`,
`gitleaks` and CodeQL already gate every change and cover CIS-benchmark ground
better than an LLM will. **Do not re-run them** — a finding checkov would raise
is noise, since it either already failed the build or was already allowlisted
with a reason. This agent covers what a linter **cannot express**: the
invariants this project decided on and wrote down.

**Grounds itself first** in ADR 0014, ADR 0015, `docs/ci-cd/pipelines.md`
(including the ratified drift ledger), and `docs/azure/bootstrap.md` — so it
knows what is manually bootstrapped and therefore absent from Terraform on
purpose.

**The eight invariants.**

1. **No long-lived secrets** — OIDC only; system-assigned MIs; Key Vault
   *references*. Flags SP passwords, credentialed connection strings, PATs.
2. **OIDC subjects are scoped** — `repo:…:environment:prod`, not a broad branch
   wildcard. A prod identity trusting any branch is the finding that matters most.
3. **Switch-together** — no path may combine a UAT backend with PROD credentials.
4. **PROD never builds** — promotion by manifest digest, never tag; a rebuild is
   different bytes, which is precisely what ADR 0015 rejected.
5. **Exactly one cross-environment edge** — the read-only UAT `AcrPull`. A
   second, or a write-capable one, or a manual grant, is a finding.
6. **The image treaty** — flags removal or widening of `ignore_changes`, or a
   second exemption without an ADR. Knows `postgres`'s is unrelated.
7. **Least-privilege role assignments** — scoped to the resource, traceable to a
   comment or ADR.
8. **Ingress and data protection** — Worker ingress disabled, API HTTPS-only,
   no public Postgres/Redis, diagnostics to Log Analytics.

**Output.** **APPROVE** or **BLOCK — n must-fix**, then per finding: what, why
(citing the ADR), *impact as an actual consequence*, fix, severity. The
instruction is blunt: "Ensure least privilege" is not a finding; naming the
resource-group-scoped `AcrPull` at a file:line is. Closes by naming what it did
not cover.

---

### `observability-reviewer` — 89 lines

```yaml
---
name: observability-reviewer
description: Reviews handlers, endpoints and adapters for observability gaps — missing OpenTelemetry spans, absent metrics, unstructured or duplicated logs, and redaction concerns. Use after implementing a Wolverine handler, HTTP endpoint or external adapter, or as the observability lens of a diff self-review.
tools: Read, Grep, Glob, Bash
model: sonnet
---
```

**What it does not cover, stated first.** CA1848 and CA1873 are **compile
errors** here — `TreatWarningsAsErrors=true` plus
`AnalysisMode=AllEnabledByDefault` mean code using `logger.LogInformation(...)`
convenience overloads, or passing `exception.GetType().FullName` as a parameter,
**does not build**. An agent reporting them reports something `dotnet build`
already refused to produce. Stripped, so the agent spends its context on
judgment the compiler cannot make.

**Grounds itself** in ADR 0013, `docs/worker.md`, and
`src/CurrencyTracker.ServiceDefaults/` — recommending an instrument
`ServiceDefaults` already registers is a false positive.

**The four questions, per operation.**

| | Yes for | No for |
| --- | --- | --- |
| **Span** | Outbound HTTP, DB round-trips not already instrumented, message handling, cache lookups where a miss matters | In-memory sub-millisecond work — **say so and why**, don't stay silent |
| **Metric** | Counters for occurrences, histograms for durations/sizes, gauges for levels | Anything nobody would alert or dashboard on |
| **Log** | Meaningful state changes | Loop iterations. Watches for the real failure: **logging the same error twice** — logged once, at the boundary that handled it |
| **Redaction** | PII, tokens, credentials, raw input, request bodies, connection strings, JWT contents | — |

Tags must be low-cardinality: `currency.code` is fine, a raw user id is a
cardinality bomb.

**Output.** Verdict, then per gap: file:line, what is missing, *what you cannot
answer in production without it* (concretely — "you cannot tell a Frankfurter
timeout from a 500 from the logs"), and the actual code in the file's existing
style. Then a **"Deliberately not instrumented"** list — as valuable as the
gaps, because it stops the next reviewer re-raising the same thing.

---

## 6. Deliberately not built

Load-bearing section. Without it, a future session helpfully re-adds these, and
each is a second copy of something that already exists.

| Not built | Because |
| --- | --- |
| `testing-conventions` skill | 100% duplication — `AGENTS.md` already has §Testing conventions, §Test-writing rules and §Fakes live with tests. |
| `dotnet-quality-gates` skill | Its prose is `AGENTS.md` §Quality gates verbatim. The executable half became `/gates`. |
| `issue-workflow` skill | `docs/workflow.md` already holds the eight-step loop. A skill restating it is exactly the drift this setup prevents. |
| `architecture-reviewer` agent | Mechanical half is a **build gate** — `CurrencyTracker.Architecture.Tests` + NetArchTest check the dependency rule in IL on every `dotnet test`. Judgment half ("is this ADR-worthy?", "is this abstraction speculative?") needs the conversation a subagent cannot see. |
| General `security-reviewer` agent | Built-in `/security-review` already walks pending branch changes. What it cannot know is this repo's treaty — hence the narrower `azure-posture-reviewer`. |
| CA1848/CA1873 in `observability-reviewer` | Compile errors. The build reports them first. |
| Eleven mindset skills | `AGENTS.md` argued against this before the setup existed and was right: six of the eleven only identified a directory, which the Project layout table states and NetArchTest enforces. The taxonomy is now five rows, each routing to something real. |
| Anything frontend | Phase 16, unstarted, no `/web`, no `package.json`. The user-level `frontend-design` plugin covers it if it lands. |
| Duplicates of `Explore` / `Plan` / `general-purpose` | Built in. |

---

## 7. What is committed, and what is not

`.gitignore` re-includes exactly three directories:

```gitignore
/.claude/*
!/.claude/agents/
!/.claude/skills/
!/.claude/commands/
```

**`/.claude/*`, not `/.claude/`.** Git does not descend into an ignored
*directory*, so a negation under a bare `/.claude` silently re-includes nothing.
Ignore the contents, then negate. Last matching pattern wins, so a negation must
also sit *after* any broader rule that would re-catch it.

A blanket `*.json` rule lived in `.gitignore` until 15.1. It was redundant —
`[Bb]in/` and `[Oo]bj/` already cover build output — while silently untracking
`.mcp.json` and arming a trap for Phase 16 (`package.json`, `tsconfig.json`).
Removed.

**Machine-local, deliberately untracked:** `settings.json`,
`settings.local.json`, `vs-permission-hook.ps1`, `vs-debug-context-hook.ps1`,
`vs-usage-hook.ps1`, `vs-mcp-shim.ps1`, `csharpier-format-hook.ps1`.

Committing `settings.json` would be actively harmful: its hooks invoke local
scripts by relative path, so a fresh clone blocks every `Edit`/`Write` on a
`PreToolUse` hook whose target does not exist — with `"timeout": 86400`.

### The csharpier hook — reapply on a new machine

A `PostToolUse` hook formats every `.cs` file Claude touches, turning the
`_reusable-format.yml` gate from something an agent must remember into something
that already happened. A skill can only *remind*; a hook *formats*.

Add to `.claude/settings.json`:

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

`.claude/csharpier-format-hook.ps1` (50 lines) reads the hook payload from
stdin, exits early unless `tool_input.file_path` is an existing `.cs` file
inside the repo, then runs `dotnet csharpier format <file>` from the repo root.
**Fail-open** — every path exits 0, because a formatter is never a reason to
block an edit that already succeeded.

Two things that will waste your time if you rewrite it:

- csharpier is a **local** tool, so `dotnet csharpier`, and `format` is a
  **subcommand** — CSharpier 1.x has no `--write`/`--check` flags.
- **csharpier honours `.gitignore`.** Testing it on a file inside `.claude/`
  reports "Formatted 0 files" and looks like a broken hook.

---

## 8. Adding and retiring

The three-times rule, borrowed from `docs/prompts.md`, which had the right
instinct.

**Add** when you have explained the same thing to an agent **three times across
different sessions** — and only after checking it isn't already in `AGENTS.md`.
Below three, it isn't reusable yet.

- One capability per artifact. A skill over ~150 lines is probably two.
- The `description` is the firing mechanism for a skill. Write it as trigger
  conditions — *"Use whenever creating, editing or reviewing any file under
  `.github/workflows/`"* — not as a summary.
- Reviewers stay read-only: `tools: Read, Grep, Glob, Bash`.
- New agents and skills go on the protected-file list in `AGENTS.md` §Don't.

**Retire** when it hasn't fired in two phases. Delete it — don't archive, don't
comment it out. Git keeps the history; the roster should reflect current
practice. Then remove its row from `SKILLS.md`, this file, and the routing table
in `CLAUDE.md`.

### Frontmatter contract

```yaml
# .claude/agents/<name>.md
---
name: kebab-case                    # matches the filename
description: What it does + when to use it.
tools: Read, Grep, Glob, Bash       # omit to inherit all
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

---

## 9. Verifying the setup

The ignore rules are the most likely thing to be silently wrong:

```bash
git check-ignore -v .claude/agents/docs-drift-auditor.md   # expect: no match
git check-ignore -v .claude/settings.json                  # expect: /.claude/*
git check-ignore -v .mcp.json                              # expect: no match
git status --short -uall .claude/                          # expect: exactly 9 .md files
```

Artifacts load — run `/context` and confirm the 3 skills and 3 commands appear,
and that `CLAUDE.md`'s `@AGENTS.md` import resolves (AGENTS.md *content* in
context, not just the filename). **Newly added agents and skills register at
session start**, so restart before expecting them.

The acceptance test is `/drift` returning **CLEAN** — and not re-reporting the
rows in `docs/ci-cd/pipelines.md` §Recorded drifts.

---

## Related

- [`SKILLS.md`](../../SKILLS.md) — the one-page index.
- [`AGENTS.md`](../../AGENTS.md) — canonical conventions; §Don't holds the protected-file list.
- [`CLAUDE.md`](../../CLAUDE.md) — the routing table.
- [`docs/workflow.md`](../workflow.md) — the eight-step per-issue loop.
- [`docs/prompts.md`](../prompts.md) — paste-ready prompts for other runtimes.
- ADR [0016](../decisions/0016-agent-configuration-in-claude-dir.md) — why this setup looks like this.
