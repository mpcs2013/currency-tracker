---
description: Audit the project docs for drift against what the repo actually contains
allowed-tools: Agent, Bash, Read, Grep, Glob
---

Dispatch the **`docs-drift-auditor`** agent.

Scope: $ARGUMENTS — if empty, audit the full default set (`AGENTS.md`,
`CLAUDE.md`, `SKILLS.md`, `README.md`, `docs/ci-cd/pipelines.md`).

Give the agent a fresh context — do **not** summarise this conversation into the
prompt. Its value is that it checks the docs against the repo rather than
against what we have been assuming for the last hour. Pass it the scope and
nothing else.

When it reports back:

- Present its findings verbatim-in-substance. Don't soften a drift into a
  suggestion.
- **Do not fix anything yet.** Most of the files in scope are on the
  protected-file list in `AGENTS.md` §Don't and need an issue naming them as a
  deliverable. Report first; ask before editing.
- If it returns clean, say so plainly. A clean run is the point.
