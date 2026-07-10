# Azure deploy run - 2026-07-10

A supervised deploy-capture-destroy session on the Microsoft Azure Sponsorship subscription
(uksouth). This records the process, the issues found on Azure, the fixes made, and the results.

## Pre-flight

- Subscription: Microsoft Azure Sponsorship (Enabled).
- Resource providers registered: CognitiveServices, App, DBforPostgreSQL, Search,
  OperationalInsights, Insights, KeyVault, ManagedIdentity.
- Docker daemon available; GitHub token lacks `write:packages`, so the container image is built and
  pushed with Azure Container Registry (`az acr build`) rather than GHCR.

## Issue 1 - model quota / SKU on the Sponsorship subscription

The planned deployments did not match available quota in uksouth:

| Model | Planned | Quota found | Fix |
| --- | --- | --- | --- |
| chat | `gpt-4o-mini` GlobalStandard | GlobalStandard `gpt-4o-mini` = **0** | Use `gpt-4o` `2024-11-20` on **Standard** (quota 150) |
| embedding | `text-embedding-3-small` Standard | Standard `text-embedding-3-small` unavailable | Use **GlobalStandard** (quota 1000) |

Changes: `infra/ai_foundry.tf` (model versions and SKUs) and `infra/variables.tf`
(`chat_deployment` default `gpt-4o`). Added `infra/acr.tf` (Container Registry + AcrPull for the
app identity) and pointed the Container App at the ACR image.

## Issue 2 - no real-time chat quota on the Sponsorship subscription

Neither `gpt-4o` (2024-11-20) nor `gpt-4.1-mini` (2025-04-14) could be deployed:
`ServiceModelDeprecating: ... is in deprecating state and cannot be used for new deployments`. On
this subscription every GA chat model is GlobalStandard-only with **0** quota (in uksouth and every
region checked), and the Standard-SKU models are past their new-deployment cutoff. Embeddings
(`text-embedding-3-small`, GlobalStandard, quota 1000) deploy fine.

**Fix / decision:** deploy with real Azure embeddings driving retrieval, and the deterministic local
synthesiser for the answer wording (`Ai:Mode=Local`, `Ai:Embeddings:Provider=Azure`). The code
targets an Azure chat deployment unchanged when chat quota is available. The `azurerm_cognitive_deployment.chat`
resource is removed; `container_app.tf` sets the hybrid env.

## Issue 3 - Foundry project requires allowProjectManagement

`azurerm_cognitive_account_project` failed: *Project can only be created under AIServices Kind
account with allowProjectManagement set to true* - which `azurerm_cognitive_account` (v4.80) does
not expose. The RAG uses the account's Azure OpenAI endpoint directly and does not need a project,
so it is omitted; the server-managed Foundry agent path is out of scope for this deploy.

## Results

Deployed resources (rg-fca-dev-uks-01, uksouth): AI Foundry account + `text-embedding-3-small`
deployment, Postgres Flexible + pgvector, Container Apps (scale-to-zero), AI Search, Content Safety,
Log Analytics + App Insights, Key Vault + managed identity, Container Registry.

- **App URL:** https://ca-fca-dev-uks-01.jollyglacier-4b42e05d.uksouth.azurecontainerapps.io
- **Data:** the 10 curated provisions ingested as **real FCA Handbook text** (headless-browser fetch),
  embedded with **Azure text-embedding-3-small (1536)** into cloud Postgres.
- **E2E smoke tests** (see `smoke-tests.txt`): all 5 in-scope questions cited the correct provision,
  both out-of-scope questions refused, the agent endpoint answered, and the Blazor UI served (200).
- **Live eval against the deployed app** (see `live-eval-report.md`): **refusal correctness 100%,
  citation recall 100%** across the 9 gold cases - real Azure semantic retrieval over real FCA text.
- **Screenshots:** `screenshots/blazor-grounded-answer.png` (grounded, cited answer),
  `screenshots/blazor-refusal.png` (refusal guardrail).
- **Telemetry:** the app exports OpenTelemetry to Application Insights (connection string injected);
  metrics appear after the usual ingestion delay.
- **Cost:** consumption/serverless tiers; embeddings are pay-per-token (pennies for ~30 calls),
  Container Apps scale-to-zero, burstable Postgres. The Sponsorship offer has no Cost Management API,
  so the balance is watched in the Sponsorships portal. Everything is destroyed at the end.
