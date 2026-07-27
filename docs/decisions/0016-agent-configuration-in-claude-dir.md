# 0016 — Agent configuration lives in `.claude/` and is committed

Date: 2026-07-27 · Status: accepted · Phase: 15.1 (no milestone — see Consequences)

## Decision

Reusable agent behaviour is expressed as harness artifacts under `.claude/`,
committed and code-reviewed like any other source:

- **Commands** (`.claude/commands/`) — deliberately invoked, argument-driven:
  `/gates`, `/adr`, `/drift`.
- **Skills** (`.claude/skills/<name>/SKILL.md`) — procedural knowledge that fires
  on its own `description`: `ci-workflow-authoring`, `terraform-module`,
  `wolverine-handler`.
- **Agents** (`.claude/agents/`) — read-only reviewers in their own context:
  `docs-drift-auditor`, `azure-posture-reviewer`, `observability-reviewer`.

`.gitignore` re-includes exactly those three directories. Everything else in
`.claude/` — `settings.json`, `settings.local.json`, the four `vs-*.ps1` Visual
Studio bridge scripts — stays machine-local, because those hooks invoke local
scripts by relative path and would block every `Edit`/`Write` on a fresh clone
with a 24-hour hook timeout.

The selection rule is one sentence: **an artifact must carry knowledge that
`AGENTS.md` does not.** `AGENTS.md` loads into every session, so a skill that
restates it is a second copy that will drift.

The mindset taxonomy in `AGENTS.md` shrinks from eleven rows to five. The six
cut rows (Domain, Application, Infrastructure, API, Worker, Frontend) only
identified a directory, which the Project layout table states and NetArchTest
enforces in IL.

`docs/prompts.md` and `docs/workflow.md` are **kept**, with cross-links naming
what supersedes each prompt.

## Rejected

- **A skill per mindset (eleven skills).** `AGENTS.md` itself argued against
  this — "you don't create eleven Claude Projects" — and it was right for a
  reason that outlives the paste-buffer era: six of the eleven carry no
  behaviour a router needs, and the other five already had homes.
- **`testing-conventions` and `dotnet-quality-gates` skills.** Their entire
  content is `AGENTS.md` §Testing conventions, §Test-writing rules, §Fakes live
  with tests, and §Quality gates — verbatim. The executable half of the latter
  became `/gates`; the prose half needed no second home.
- **An `architecture-reviewer` agent.** Its mechanical half is a build gate
  (`CurrencyTracker.Architecture.Tests` + NetArchTest run on every
  `dotnet test`), and an LLM re-checking a compiler-enforced invariant is
  theatre. Its judgment half — "is this ADR-worthy", "is this abstraction
  speculative" — needs the design rationale the main thread just produced and
  a subagent cannot see. Splitting it off would have handicapped it on purpose.
- **A general `security-reviewer` agent.** The built-in `/security-review`
  already walks pending branch changes. What it cannot know is *this repo's
  treaty* — the ADR 0014 posture, OIDC subject scoping, digest-not-tag
  promotion, the one deliberate UAT→PROD `AcrPull` edge from ADR 0015 — so the
  agent was narrowed to `azure-posture-reviewer` over `infra/**` and the deploy
  workflows, and defers app code to the built-in.
- **CA1848 / CA1873 coverage in `observability-reviewer`.** With
  `TreatWarningsAsErrors` and `AnalysisMode=AllEnabledByDefault` those are
  compile errors. An agent reporting them reports something `dotnet build`
  already refused to produce.
- **Deleting `docs/prompts.md` and `docs/workflow.md`.** `prompts.md` authorises
  its own deletion once its prompts stop being pasted, and every prompt now has
  a replacement. But the replacements only exist *in Claude Code*; the prompts
  remain the correct interface for plain Claude chat, Copilot and Cursor, which
  this project also uses. Cross-linked rather than removed.
- **Committing `.claude/settings.json`.** See Decision. Its only current content
  is three hooks pointing at machine-local scripts.

## Consequences

- The setup is reviewable and survives a clone, but only for Claude Code. Other
  runtimes still get `AGENTS.md` + `docs/prompts.md`, which is why those stay.
- `.claude/agents/**`, `.claude/skills/**`, `.claude/commands/**` and `SKILLS.md`
  join the protected-file list in `AGENTS.md` §Don't. They are canon now and
  drift there costs what drift in `AGENTS.md` cost.
- The csharpier `PostToolUse` hook is machine-local by consequence of keeping
  `settings.json` untracked. `SKILLS.md` records the hook block verbatim so it
  can be reapplied. This is a real gap: on a fresh machine the formatting gate
  is a reminder again, not a guarantee.
- This work landed without a GitHub issue or milestone. All 16 milestones
  (Phase 0 → 14, 338 issues) closed before it started and no next phase was
  open, so the eight-step loop's "paste the issue" step had nothing to paste.
  `AGENTS.md` §Don't requires an issue naming a protected file as deliverable
  before editing it; this ADR is the standing record instead. If a Phase 15
  milestone is opened, backfill an issue referencing this ADR.
- `docs/decisions/0014-OIDC-posture.md` breaks the lowercase-kebab filename
  convention the other 15 ADRs follow. Noted, deliberately not renamed here —
  renaming it would break the inbound links in ADR 0015 and `pipelines.md`.

## Related

- [`SKILLS.md`](../../SKILLS.md) — the roster, and the rule for adding to it.
- [`AGENTS.md`](../../AGENTS.md) — canonical conventions; §Don't holds the
  protected-file list this ADR extends.
- [`docs/prompts.md`](../prompts.md) — the superseded-but-retained prompt catalogue.
- ADR [0003](0003-wolverine-no-marker-interfaces.md), [0006](0006-wolverine-service-location-for-internal-adapters.md) — encoded in the `wolverine-handler` skill.
- ADR [0014](0014-OIDC-posture.md), [0015](0015-deploy-topology.md) — encoded in the `azure-posture-reviewer` agent.
