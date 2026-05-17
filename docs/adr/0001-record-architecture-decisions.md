---
status: "accepted"
date: 2026-05-17
decision-makers: ["@mpcs2013"]
consulted: []
informed: []
---

# Record architecture decisions

## Context and Problem Statement

This codebase will accumulate architectural decisions over its lifetime — choice of framework, dependency direction, persistence strategy, auth provider, deployment shape. Each decision will, eventually, be questioned ("why did we use X?") or revisited ("should we replace X with Y?"). Without a durable record of the reasoning, every revisit costs an archaeology session.

How should we record those decisions?

## Considered Options

- **Markdown Architectural Decision Records (MADR 4.0)**, numbered, append-only, in `docs/adr/`.
- **Free-text notes** in a single `docs/decisions.md` file.
- **A Confluence / Notion page** outside the repo.
- **No explicit decision log** (decisions live in commit messages, PR descriptions, or memory).

## Decision Outcome

Chosen option: **MADR 4.0**, because:

- The records live with the code, version-controlled and reviewable as PRs alongside the change that introduces them.
- The numbered, append-only convention makes the decision log durable: a decision is *superseded* by a later ADR, never edited away.
- MADR 4.0's structure (status, context, considered options, outcome, consequences) is a known shape; reviewers don't have to learn a new format per ADR.
- The cost is one short Markdown file per decision. Friction is low; discipline-by-default.

### Conventions

- Files live in `docs/adr/` and are named `NNNN-kebab-case-title.md`, starting at `0001`.
- Front matter: `status`, `date`, `decision-makers` (required); `consulted`, `informed` (optional).
- Status lifecycle: `proposed → accepted → deprecated → superseded by NNNN`. Status transitions edit only the status line; the body is append-only.
- An ADR is required whenever a PR introduces an architectural decision that future PRs will be constrained by. The PR template's ADR gate enforces the question.

### Consequences

- Good, because every architectural choice has a discoverable rationale.
- Good, because the "should we revisit X?" conversation starts with the original reasoning, not a guess at it.
- Bad, because writing an ADR adds 30 minutes to the PR that introduces the decision. The alternative — *not* writing it — costs hours every time the decision is questioned later. Trade-off favours the ADR.

## More Information

- [MADR 4.0 templates](https://github.com/adr/madr/tree/4.0.0/template) — full and minimal variants.
- [Documenting Architecture Decisions (Michael Nygard, 2011)](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) — the original ADR proposal.
- The next ADR — [0002-use-clean-architecture.md](0002-use-clean-architecture.md) — is the first to use this format.