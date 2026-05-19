# Reusable prompts

This catalogue is intentionally small: add a prompt only after you've pasted the same shape **≥3 times** across different sessions, and remove a prompt when you **haven't pasted it in two phases**. Six prompts that keep getting used beat a longer list that drifts.

## 1. Plan the issue

Use this when you want the agent to frame the work before it writes anything.

```text
Mindset: <relevant mindset(s)>. Issue: <paste the GitHub issue here>.

Explain the goal in plain English in 3-5 sentences.
List the files to create or modify, in the order you would touch them.
Flag prerequisite concepts or unknowns I should verify first.
Call out architectural, security, and observability concerns before any code.
Stop after the plan. Do not generate code until I confirm.
```

## 2. Write failing tests for this issue

Use this when the issue has testable behaviour and you want the red step before the green one.

```text
Mindset: Testing + <relevant mindset(s)>. Issue: <paste the GitHub issue here>.

Write the failing tests for this issue first using xUnit v3.
Name the test cases you would add, explain what each proves, and then show the test code.
Do not write or modify production code yet.
Call out any missing fixtures, test data, or seams the implementation will need.
```

## 3. Review this diff (architectural / security / observability)

Use this when you already have a diff and want a harsh review through the project's three required lenses.

```text
Mindset: Security + Observability + <relevant mindset(s)>.

Review this diff as a third-party reviewer.
Check architectural fit against the project boundaries, then review for security concerns, then for observability gaps.
Call out anything that should trigger an ADR or an AGENTS.md update.
Report findings as: blocking issues, non-blocking suggestions, and things that look good.
Diff:
<paste the diff here>
```

## 4. OWASP walk this PR

Use this when a PR changes an API, auth flow, boundary, or any request/response handling.

```text
Mindset: Security. PR: <paste PR link, diff, or summary here>.

Walk this PR against the current OWASP API Security Top 10.
Specifically check for BOLA, Excessive Data Exposure, Mass Assignment, and Improper Inventory Management.
Also call out authn/authz mistakes, secret handling, unsafe defaults, missing validation, and risky logging.
Classify findings by severity, point to the exact line or behaviour, and propose the smallest safe fix.
```

## 5. Find observability gaps in this handler

Use this when a handler or endpoint works functionally but may be thin on spans, logs, or failure visibility.

```text
Mindset: Observability.

Review this handler for observability gaps.
Identify missing spans, missing structured logs, missing metrics, and places where `traceId` should flow but does not.
Point out where errors should be logged once at the handling boundary and where retries or background work need correlation.
Handler:
<paste the handler, endpoint, or workflow here>
```

## 6. Security-posture review of this Terraform module

Use this when a Terraform module touches identity, networking, secrets, storage, or internet-facing infrastructure.

```text
Mindset: Azure + Security.

Review this Terraform module for security posture.
Check identity and RBAC, secret handling, network exposure, encryption settings, diagnostics, policy fit, and least-privilege defaults.
Call out anything that would block production use, anything acceptable only for local/dev, and any missing guardrails.
Module:
<paste the Terraform module here>
```

## Adding a prompt

This file is living memory. When you (the agent or the human) discover a prompt shape worth keeping:

- if you've pasted the same shape **≥3 times** across different sessions, add it;
- keep it to one sentence on when to use it plus one fenced block with `Mindset:` first;
- prefer replacing or tightening an overlapping prompt instead of growing the catalogue by default.

## Removing a prompt

Drift is the failure mode. When a prompt stops earning its place:

- if you haven't pasted it in two phases, remove it;
- if two prompts now overlap, keep the one that still gets used and delete or merge the other;
- if a prompt's wording changed enough that the old shape no longer gets pasted, replace it rather than keeping both.
