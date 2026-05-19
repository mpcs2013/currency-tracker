# Per-issue workflow

This file describes how a single GitHub issue moves from "open" to
"squash-merged green" in this project. It is the **human-facing companion**
to `AGENTS.md`: the principles and mindset taxonomy live there, the loop
that uses them lives here.

The loop is **guidance, not ceremony**. A one-line config tweak does not
need all eight steps. A fourteen-file refactor needs every one. The
"Compress as you internalise" note below names which steps are skippable
for which size of change.

## The eight-step loop

1. **Paste the issue** into a fresh agent session along with the relevant
   mindset(s) from the taxonomy in
   [`AGENTS.md`](../AGENTS.md#the-eleven-mindset-taxonomy). Use the
   prompting pattern below.
2. **Ask the agent to plan first.** Specifically: explain the goal in
   plain English (3–5 sentences); list files to create or modify, in the
   order to do them; flag prerequisite concepts; name any architectural,
   security, or observability concerns *before* writing code. Read the
   plan critically. Reject and refine if it skips the failing-test step
   for a behavioural change.
3. **Tests first** when the issue has testable behaviour. Red, then green,
   then refactor. The failing test is committed before the implementation
   commit so the diff order in the PR tells the story.
4. **Implement to pass.** Single concept per PR. If you find yourself
   thinking *while I'm here…*, stop and open another issue.
5. **Push, open a draft PR.** Draft, not ready — the draft state is the
   signal to yourself and to CI that the diff is not yet finished. Watch
   the `ci` workflow run; address failures before doing anything else.
6. **Self-review the diff** through three lenses: architectural
   (boundaries respected? new dependencies justified?), security (any
   new attack surface? secrets in logs? input validation?),
   observability (meaningful operations spanned? state changes logged
   with `traceId`?). You can outsource this review to the agent by
   pasting your own diff back and framing it as a third-party reviewer
   — `Mindset: Security. Review this diff for OWASP top-10 concerns.`
7. **Update `docs/decisions/`** if the PR introduces an architectural
   choice that future-you might question. Update `AGENTS.md` (Conventions,
   Gotchas, or Don't) if the session surfaced a new project-specific rule.
   Both updates go in the same PR — they're part of the work.
8. **Mark PR ready, wait for `ci` green, squash-merge.** Linear history
   on `main`. The branch auto-deletes on merge.

## The prompting pattern

Every session opens with one line:

    Mindset: Application + Security. Issue: <paste the GitHub issue here>.

Two fields, both labelled, mindset first. The mindset narrows the agent's
interpretive frame *before* it reads the issue text — reversing the order
leaks training-data instincts into the early reading. The `+` combines
mindsets when an issue spans two; `,` and `/` work but are less consistent
across tool runtimes, so stick with `+`.

For more elaborate sessions (a multi-file refactor, an OWASP audit, a
Terraform module review), the catalogue in
[`docs/prompts.md`](./prompts.md) has paste-ready longer prompts that
extend this pattern.

## Compress as you internalise

The loop above is the shape for a *new-concept* PR. Most PRs are smaller
than that, and the steps that are skippable scale with the change:

| Change shape                            | Skippable steps                                   |
| --------------------------------------- | ------------------------------------------------- |
| Typo / one-line README tweak            | 2 (plan), 3 (tests), 7 (ADR) — skim 6 and ship.   |
| Config tweak (e.g. `.editorconfig` row) | 3 (no behaviour), often 7 (no architecture).      |
| Single-file new feature with tests      | None — this is the full eight-step shape.         |
| Multi-file refactor / framework intro   | None, and step 7 is the most important one.      |

If you find yourself writing a four-paragraph ADR for a typo PR, you're
over-applying the loop. If you find yourself skipping step 3 on a new
handler "because the change is small", you're under-applying it.

## See also

- [`AGENTS.md`](../AGENTS.md) — project memory, principles, and the
  mindset taxonomy.
- [`docs/prompts.md`](./prompts.md) — paste-ready reusable prompts.
- [`docs/decisions/`](./decisions/) — architecture decision records.
