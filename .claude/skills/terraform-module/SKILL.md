---
name: terraform-module
description: House pattern for the Azure Terraform under infra/terraform/ — the main/variables/outputs module triple, the uat/prod envelope split, OIDC-only identity, the container-image treaty, and the gates that run on every plan. Use whenever creating, editing or reviewing any .tf/.hcl file, or wiring a new Azure resource.
---

# Terraform in this repo

```
infra/terraform/
  main.tf variables.tf output.tf providers.tf versions.tf backend.tf
  .terraform.lock.hcl          ← committed on purpose; never delete to "fix" a plan
  envs/uat/{backend.hcl,terraform.tfvars}
  envs/prod/{backend.hcl,terraform.tfvars}
  modules/<12 modules>/{main.tf,variables.tf,outputs.tf}
```

Modules: `network`, `acr`, `postgres`, `redis`, `keyvault`, `app-insights`,
`log-analytics`, `storage-logs`, `container-apps-env`, `container-app-api`,
`container-app-worker`, `role-assignments`. Read the closest one before writing.

## Module shape

Exactly three files — `main.tf`, `variables.tf`, `outputs.tf`. No `providers.tf`
inside a module; providers are configured once at the root.

`main.tf` opens with a comment stating what the module owns **and what it
deliberately does not**:

```hcl
# The API Container App. Terraform owns the infrastructure shape; the deploy
# pipeline owns the image (ignore_changes below is the treaty). The system-
# assigned identity minted here is the principal every 14.24 grant targets.
```

## Variable descriptions carry the reasoning

This is the repo's strongest Terraform habit. A `description` is not a restated
name — it says why the default is what it is, what it couples to, and when to
change it:

```hcl
variable "use_acr_registry" {
  description = "Declare the ACR registry credential on the app. Must stay false until 14.24's AcrPull grant exists for this app's identity — see the bootstrap-cycle note in main.tf. Flip it in the same change that points `image` at the ACR."
  type        = bool
  default     = false
}
```

Every variable gets `description` and `type`. Match this depth — a one-word
description is a defect here.

## The image treaty

```hcl
lifecycle {
  ignore_changes = [
    template[0].container[0].image,
  ]
}
```

Terraform owns infrastructure shape; **the deploy pipeline owns the image**. The
`image` variable's default is a public bootstrap placeholder
(`mcr.microsoft.com/dotnet/samples:aspnetapp`) used only for first apply.

Consequences to respect:

- Never remove or widen that `ignore_changes` to "make a plan clean".
- Never add a pipeline step that sets an image through Terraform.
- Adding a second exemption to the treaty is an ADR-worthy decision. ADR 0015
  rejected multiple-revision traffic splitting partly because it would have
  forced one.

`postgres` also uses `ignore_changes` (on `zone` and
`high_availability[0].standby_availability_zone`) for a different reason — Azure
moves them on failover, per documented provider guidance. Different rationale,
same rule: don't touch it without understanding which one applies.

## Identity: OIDC, no long-lived secrets

ADR 0014 is the posture. In practice:

- Container Apps use **system-assigned managed identities**; the identity minted
  in the app module is the principal `modules/role-assignments` targets.
- Secrets are Key Vault references, not values in tfvars or app settings.
- Pipelines authenticate with `ARM_USE_OIDC` + `ARM_CLIENT_ID` / `ARM_TENANT_ID`
  / `ARM_SUBSCRIPTION_ID` from `vars`. There is no service-principal password
  anywhere, and adding one is not a shortcut — it is a posture change.
- The one deliberate cross-environment edge is PROD's read-only `AcrPull` on the
  **UAT** ACR, for digest promotion (ADR 0015). It lives in the Terraform ledger
  rather than as a manual grant so it stays reviewable. Don't add a second
  cross-environment grant without an ADR.

## The environment envelope

`envs/uat/` and `envs/prod/` each hold `backend.hcl` + `terraform.tfvars`, and
`_reusable-terraform.yml`'s single `environment` input selects backend, tfvars,
OIDC trust and `AZURE_*` vars **together**. Never let those be chosen
separately — mixing a UAT backend with PROD credentials is the failure that rule
prevents.

New environment-varying value → add it to **both** tfvars files in the same PR,
with a `variable` block at the root that has no default if it must be explicit.

## Gates that already run

`_reusable-terraform.yml` runs `fmt` → `init` → `validate` → `tflint` →
`checkov` → `plan` (→ `apply`). `terraform-pr.yml` runs the whole thing at
`apply: false` for any PR touching `infra/**` and upserts the plan as a sticky
comment.

So: run `terraform fmt -recursive` before you finish, and **don't hand-audit
what checkov and tflint already cover**. If checkov flags something, fix it or
add a documented inline exception — don't silence the tool.

## Don't

- Don't delete `.terraform.lock.hcl` or run `init -upgrade` to clear an error.
  It is committed deliberately; provider drift is the thing it prevents.
- Don't add a provider or module source without an ADR (`AGENTS.md` §Don't
  covers Terraform modules explicitly).
- Don't commit state. The backend is `azurerm`; a local `terraform.tfstate` is
  never a commit.
- Don't put a secret value in `terraform.tfvars`. Key Vault reference or
  pipeline variable.
- Don't apply from a workstation against PROD. `deploy-prod.yml` and its
  environment approvals are the path.

## Related

- ADR [0014](../../../docs/decisions/0014-OIDC-posture.md) — identity posture.
- ADR [0015](../../../docs/decisions/0015-deploy-topology.md) — deploy topology, digest promotion.
- [`docs/azure/bootstrap.md`](../../../docs/azure/bootstrap.md) — the manual OIDC/state bootstrap.
- [`docs/ci-cd/pipelines.md`](../../../docs/ci-cd/pipelines.md) — where the gates run.
