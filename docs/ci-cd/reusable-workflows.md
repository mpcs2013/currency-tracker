# Reusable workflows — the rules of the road

`.github/workflows/_reusable-*.yml` is a small library of jobs, each one
focused on doing one thing well. The four leaf workflows in Phase 0.D
(`pr-validation.yml`, `main-ci.yml`, `deploy-uat.yml`, `deploy-prod.yml`)
compose them. This document describes the conventions every reusable in the
family must follow.

## Naming conventions

- File name: `_reusable-<purpose>.yml`. The leading underscore is the visual
  signal that this workflow is a library, not a trigger target. The hyphen-
  separated purpose name matches the job name and the eventual status-check
  name.
- The `name:` field inside the workflow matches the filename without the
  `.yml` extension (e.g. `name: _reusable-build`).
- The single job inside each reusable is named after the purpose word only:
  `build`, `test`, `format`, `codeql`, `docker-build`, `deploy`. This name
  becomes the status-check name in branch protection (Phase 0.29).
- New reusables added in later phases follow the same shape:
  `_reusable-terraform.yml` → `name: _reusable-terraform`, job name
  `terraform`.

## Trigger rules

- `on:` declares `workflow_call` **only**. No `push`, no `pull_request`, no
  `workflow_dispatch`, no `schedule`. A reusable that's also directly
  triggerable becomes runnable without inputs and clutters the run history.
- Throwaway test callers (named `_test-<purpose>.yml`, `workflow_dispatch:`
  only) are acceptable during development but must be removed before the PR
  merges.

## Inputs and outputs

- All `workflow_call.inputs` are typed (`type: string | boolean | number`).
  YAML's `string` is the right type for tags, paths, and OS names; `number`
  for thresholds; `boolean` for feature flags.
- Input names use `kebab-case` matching the GitHub Actions ecosystem
  convention (`dotnet-version`, not `DotnetVersion` or `dotnetVersion`).
- Outputs are declared at BOTH the workflow level (`on.workflow_call.outputs`)
  and the job level (`jobs.<job>.outputs`). The workflow-level declaration's
  `value:` points at the job-level output via
  `${{ jobs.<job>.outputs.<key> }}`. Skipping the workflow-level declaration
  is the single most common authoring bug.

## Secrets handling

- Default to **explicit** secret enumeration in `workflow_call.secrets:`. A
  caller passing `secrets: inherit` then re-listing the names provides
  defence-in-depth audit.
- `secrets: inherit` is acceptable for `_reusable-azure-deploy.yml`
  specifically, because the audit surface lives in the GitHub Environment
  configuration (Settings → Environments → uat | prod).
- A reusable workflow that needs `secrets.GITHUB_TOKEN` does NOT need to
  declare it — `GITHUB_TOKEN` is an automatic context value provided to
  every workflow run.
- Never accept a credential (password, token, key) as a `workflow_call.input`.
  Inputs are logged in workflow run metadata and visible to anyone with
  `actions: read`.

## Permissions

- Every reusable declares `permissions:` at the workflow level, NOT at the
  job level (unless a specific job needs more than the workflow default).
- The minimum is `contents: read`. Add only what's required:
  - `_reusable-build.yml` → `contents: read`
  - `_reusable-test.yml` → `contents: read`, `checks: write` (for test-
    reporter steps in Phase 13)
  - `_reusable-format.yml` → `contents: read`
  - `_reusable-codeql.yml` → `contents: read`, `security-events: write`,
    `actions: read`
  - `_reusable-docker-build.yml` → `contents: read`, `packages: write`
  - `_reusable-azure-deploy.yml` → `contents: read`, `id-token: write`
- A reusable workflow does NOT inherit the caller's permissions. The caller
  must declare matching permissions on the job that calls the reusable. This
  is a frequent source of 403 errors — particularly with `security-events:
  write` on CodeQL and `id-token: write` on Azure deploy.

## Action version pinning

- Third-party actions are pinned to their major version tag (`@v6`, `@v4`,
  etc.). Major versions are stable; minor and patch updates flow in
  automatically.
- Dependabot (Phase 0.25) keeps the major versions current.
- Never use `@main`, `@master`, or `@latest`. These are supply-chain risks.
- Pinning to a SHA is acceptable for high-security contexts (Phase 14's
  Azure deploy might do this) but adds maintenance burden — every Dependabot
  PR has to bump the SHA explicitly.

## Adding a new reusable workflow

When a later phase wants to introduce a new reusable (e.g. `_reusable-
terraform.yml` in Phase 14, `_reusable-web-ci.yml` in Phase 16):

1. **File name:** `_reusable-<one-or-two-word-purpose>.yml`.
2. **Header comment:** include a top-of-file note explaining the workflow is
   a library and is not directly triggered. (Cribbed from `_reusable-
   build.yml`.)
3. **Inputs:** typed, kebab-case, with `description:` strings that read as
   user-facing help.
4. **Permissions:** minimum required. Document in this file's section above
   when a new permission is needed.
5. **Pin all third-party actions** to a major version.
6. **Add a row to the inventory below.**
7. **Test in isolation** via a throwaway `_test-<purpose>.yml` caller.

## Current inventory

| File                            | Job name        | Triggers                                              | Permissions added                                |
| ------------------------------- | --------------- | ----------------------------------------------------- | ------------------------------------------------ |
| `_reusable-build.yml`           | `build`         | called by `pr-validation`, `main-ci`                  | —                                                |
| `_reusable-test.yml`            | `test`          | called by `pr-validation`, `main-ci`                  | `checks: write`                                  |
| `_reusable-format.yml`          | `format`        | called by `pr-validation`, `main-ci`                  | —                                                |
| `_reusable-codeql.yml`          | `codeql`        | called by `pr-validation`                             | `security-events: write`, `actions: read`        |
| `_reusable-docker-build.yml`    | `docker-build`  | called by `main-ci` (Phase 14)                        | `packages: write`                                |
| `_reusable-azure-deploy.yml`    | `deploy`        | called by `deploy-uat`, `deploy-prod` (Phase 14)      | `id-token: write`                                |

## Future additions (planned)

| File                            | Phase | Purpose                                              |
| ------------------------------- | ----- | ---------------------------------------------------- |
| `_reusable-terraform.yml`       | 14.13 | fmt + init + validate + tflint + checkov + plan/apply |
| `_reusable-trivy-scan.yml`      | 14.28 | container CVE scan, fail on HIGH/CRITICAL            |
| `_reusable-acr-push.yml`        | 14.29 | OIDC → ACR push                                      |
| `_reusable-web-ci.yml`          | 16.5  | npm ci → lint → typecheck → test → build             |
| `_reusable-playwright.yml`      | 16.32 | E2E against a deployed environment                   |
| `_reusable-swa-deploy.yml`      | 16.28 | Static Web Apps deploy via OIDC                      |