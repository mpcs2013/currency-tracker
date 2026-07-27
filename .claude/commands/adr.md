---
description: Write the next architecture decision record in docs/decisions/
allowed-tools: Bash, Read, Write, Grep, Glob
---

Write an ADR for: **$ARGUMENTS**

If that is empty, ask what decision is being recorded before writing anything.

## Steps

1. `ls docs/decisions/` — take the highest number and add one. Zero-pad to four
   digits.
2. Filename: `docs/decisions/NNNN-kebab-case-title.md`, all lowercase.
   (`0014-OIDC-posture.md` breaks this; it is the exception, not the pattern.)
3. Read the two or three most recent ADRs first. The house voice is specific and
   argumentative, not a template fill-in.
4. Write the file. Do not renumber or edit existing ADRs.

## House structure

```markdown
# NNNN — <title: the decision, not the topic>

Date: YYYY-MM-DD · Status: accepted · Phase: <N.N> (issue <N>)

## Decision

<What was decided, in present tense, as a rule someone can follow. Name the
files, modules or workflows it binds.>

## Rejected

- **<The alternative, named.>** <Why it lost — the concrete mechanism, not a
  preference. "Fights the Terraform-owned traffic_weight block, which
  ignore_changes does not cover" beats "more complex".>

## Consequences

- <What this costs, including what is genuinely lost. An ADR with only upsides
  is not finished.>

## Related

- ADR [NNNN](NNNN-file.md) — <one line on the relationship>.
- [`docs/...`](../...) — <one line>.
```

## What makes these good

- **Title states the decision**, not the subject area. "Deploy topology:
  single-revision health-gated cutover, digest promotion" — not "Deployment".
- **`## Rejected` is the load-bearing section.** It is what stops a future
  session helpfully reintroducing the thing. Each entry names the alternative in
  bold and gives the mechanism that killed it.
- **`## Consequences` records the loss.** ADR 0015 ends "what is genuinely lost
  is manual canarying. If that requirement appears, this ADR is the thing to
  supersede." Write that sentence.
- Status is `accepted` unless superseding — then set the old ADR's status to
  `superseded by NNNN` and link both ways.

## Then

- If the decision adds a rule agents must follow, add a one-liner to `AGENTS.md`
  §Don't or §Conventions linking the ADR. Per `AGENTS.md` §Don't, that edit needs
  the issue to name `AGENTS.md` as a deliverable — if it doesn't, propose the
  wording in the PR description instead of editing.
- The ADR and the code it justifies go in the **same PR**. Step 7 of
  `docs/workflow.md`.
