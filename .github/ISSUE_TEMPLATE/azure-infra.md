---
name: ☁️ Azure / Infra
about: Terraform, Container Apps, Key Vault, App Insights, or other Azure resource changes.
title: "[N.X] <one-line summary>"
labels: ["needs-triage", "infra", "azure"]
assignees: ''
---

## Goal

<!-- What infrastructure changes. Module, resource, or workflow affected. -->

## Why now

<!-- What does this unblock? Which downstream phase or workflow depends on it? -->

## Blast radius

<!-- What does this change touch? "UAT only", "PROD only", "shared resource group", etc. -->

## Rollback procedure

<!-- One paragraph. If this lands and breaks something in UAT/PROD, how do we revert? -->
<!-- "git revert + terraform apply" is fine if true, but write it explicitly. -->

## Cost impact

<!-- Estimated monthly cost delta in USD. "None" is a valid answer; "I don't know" is not. -->

## Acceptance criteria

- [ ] `terraform plan` clean on the affected workspace.
- [ ] If touching a deploy workflow: a no-op PR-validation run is green.
- [ ] If touching a runbook: the runbook is updated in the same PR.
- [ ] If touching secrets handling: the Security Agent has reviewed.