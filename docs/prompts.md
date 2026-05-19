# Reusable prompts

This file is a small catalogue of prompts you paste **verbatim** into
agent sessions. It is not exhaustive — and it isn't trying to be.

**Inclusion criterion:** you have pasted the same shape at least three
times across different sessions. Below that, it's not yet reusable; keep
it in your head or in a scratch file.

**Removal criterion:** you have not pasted a prompt in two phases.
Remove it. Catalogue rot is real; an unused prompt teaches the next
session the wrong defaults.

Every prompt is a fenced code block. Triple-click to select, paste into
your agent session, fill the placeholders. The `Mindset:` field is
pre-filled where the choice is obvious; pick or extend it where it
isn't.

## 1. Plan the issue (default opener)

Use this for **step 2 of the per-issue workflow**
([`docs/workflow.md`](./workflow.md)). It is the prompt you'll paste
most.

` ```text

Mindset: <Domain | Application | Infrastructure | API | Worker | Testing | Security | Observability | Azure | Frontend | Documentation> [+ <second mindset if relevant>].
Issue: <paste the full GitHub issue body here, including Goal, Why now, Acceptance criteria>.

Before any code, walk me through:

1. The goal in plain English — 3 to 5 sentences.
2. The files you will create or modify, in the order you should do them.
3. Any prerequisite concepts I should read about first (link if you can).
4. The architectural boundaries you must not cross for this issue.
5. The security and observability concerns that apply, in one sentence each.
6. The failing tests you will write before any implementation.

Stop after the plan. Do not generate code until I confirm.

` ```

## 2. Write failing tests for this issue

Use this **immediately after** the plan in #1 is confirmed and before
any implementation. Forces a red-then-green workflow.

` ```text

Mindset: Testing + <primary mindset from the issue>.
Issue context: <paste issue + the plan you just produced>.

Write the failing tests for this issue, in the test project that
follows the dependency direction (test projects depend "upward":
Domain.UnitTests references Domain; Application.UnitTests references
Application + Domain; etc.).

For each test:

- Name it `MethodOrBehaviour_Condition_ExpectedResult`.
- Use FluentAssertions for the assertion.
- Use xUnit v3 conventions (no `[Fact]` on async-void; `Task` return type;
  `CancellationToken` flowed through if the implementation will be async).
- Do not implement anything yet — only the tests, and only enough setup
  for them to fail with a meaningful message (not just "method not found").

Show the tests as full file contents I can paste; show the expected
`dotnet test` output (red).

` ```

## 3. Review this diff

Use for **step 6 of the per-issue workflow** — self-review outsourced to
the agent. Three lenses, one prompt.

` ```text

Mindset: Security + Observability + <primary mindset from the issue>.

Review the following diff as if you were a third-party reviewer with
veto power over the merge. Apply three lenses:

1. Architectural — does any file cross a boundary defined in AGENTS.md's
   Architecture section? Any new top-level dependencies that need an ADR?
   Any abstraction added "for a hypothetical future caller"?
2. Security — any new attack surface (network ingress, deserialisation,
   SQL/Cypher/Cypher-like dynamic queries)? Secrets logged or embedded?
   Input validation correct (FluentValidation rule for every required
   property; range and length bounds where appropriate)?
3. Observability — every meaningful operation has a span? Every state
   change has a structured log line with `traceId`? Errors logged once,
   at the boundary that handled them — not duplicated up the stack?

Output a numbered list of concerns. For each one, state the file:line,
the lens (A / S / O), and the concrete change you'd suggest. End with
"OK to merge" or "BLOCK — fix N and M first".

Diff:
<paste git diff or PR diff URL here>.

` ```

## 4. OWASP walk this PR

Use **once per HTTP-facing PR** (anything that touches `Api`, an
endpoint, an auth flow, or input deserialisation). Heavier than the
review prompt — pull it out for higher-risk diffs.

` ```text

Mindset: Security.

Walk this PR against the OWASP API Security Top 10 (current). For each
of the ten categories, say: "Not applicable" or "Relevant — see file:line,
concern, suggested fix". Be specific; "ensure proper authentication" is
not a finding.

Pay particular attention to:

- Broken Object Level Authorisation (BOLA) — does any endpoint trust an
  ID from the path without checking ownership?
- Excessive Data Exposure — does any response DTO contain fields the
  caller shouldn't see?
- Mass Assignment — does any inbound DTO let the caller set fields that
  should be server-controlled (Id, CreatedAt, OwnerId)?
- Improper Inventory Management — is the OpenAPI surface accidentally
  exposing an endpoint that should be internal?

Diff:
<paste git diff or PR diff URL here>.

` ```

## 5. Find observability gaps in this handler

Use when a Wolverine handler or HTTP endpoint exists but you suspect it's
under-instrumented. Often paired with #3 after the architectural and
security lenses have come back clean.

` ```text

Mindset: Observability.

For the handler / endpoint below, list every operation that should have:

- An `ActivitySource` span (with what tags).
- A `Meter` instrument (counter / histogram / gauge — say which and why).
- A structured Serilog log line (with what properties beyond traceId).
- A redaction concern (PII, tokens, raw user input).

For each, state file:line and the exact code I'd add. If the operation
should NOT have one of the four, say so and why (e.g. "no span — this is
in-memory and sub-millisecond, span overhead would dominate").

Code:
<paste handler or endpoint here>.

` ```

## 6. Security-posture review of this Terraform module

Use when reading or writing an Azure resource in Terraform — even before
the HCL is yours to keep. Forces Microsoft's docs and the security
defaults into the conversation early.

` ```

Mindset: Azure + Security.

For the Terraform resource below, answer:

1. What identity does it use to talk to other Azure services? (Managed
   Identity? Connection string? OAuth? None?) Is that the right answer
   for this project's "no long-lived secrets" posture?
2. What network ingress does it expose by default? What would I change
   to lock it down to a private endpoint / vNet / approved IP range?
3. What encryption-at-rest / encryption-in-transit settings are
   explicit, and which are inherited from the Azure default? Are any of
   the defaults weaker than I should accept?
4. What audit / diagnostic logs does it emit, and where do they go by
   default? What would I change to route them to my Log Analytics
   workspace?
5. What are the top three resource-specific OWASP-style concerns for
   this exact resource type (not generic Azure advice — specific to
   `<resource type>`)?

Resource:
<paste HCL block or `azurerm_*` resource type>.

` ```

## Adding a prompt

When you have pasted the same shape three times across different
sessions, lift it into this file. Use the structure above: a sentence on
when to use it, then a fenced block with the `Mindset:` field pre-filled
or templated. Keep the prompt under 30 lines — anything longer is
probably two prompts.

## Removing a prompt

When you realise you haven't pasted a prompt in two phases, delete it.
Don't archive, don't comment-out — delete. Git keeps the history; the
catalogue should reflect current practice.
