# FCA Handbook Assistant - design

Date: 2026-07-10
Status: approved, in build

## Purpose

A full-stack .NET reference implementation of regulated-grade AI on Azure: grounded,
cited question-answering over the public FCA Handbook (RAG), plus a tool-calling agent
for looking up and cross-referencing specific provisions. The point is to evidence, in
one deployed artifact, production .NET, the Azure AI service layer, and LLMOps (evals,
cost observability, CI/CD) in a regulated financial-services setting.

The distinctive angle - and the thing this design optimises for - is regulated-grade
guardrails: grounded citations or a refusal (never a hallucinated rule), a per-answer
source audit log, Content Safety and PII handling on user input, and evals that test the
regulated behaviour (citation accuracy and refusal-when-unsure), not just answer quality.

## Decisions

- Go all the way to a real Azure run this session: build the core locally, then provision
  Azure, ingest, run live evals, exercise the app, capture evidence, and destroy.
- Primary retrieval is pgvector on the local Postgres (not Docker); Azure AI Search is the
  cloud parity variant behind the same interface. Both are exercised, evidencing both.
- The ingested Handbook content is a curated representative subset (PRIN in full plus
  selected SYSC / COBS / SUP chapters) - enough to demonstrate grounding and refusal, small
  to embed, and no wholesale redistribution of Crown/FCA copyright material.
- Agent is the code-first Foundry Responses agent (Microsoft Agent Framework over an Azure
  AI Foundry project), accepting that the Foundry provider is still preview. The
  server-managed Foundry Agent Service is noted in the README as the managed-runtime
  alternative.
- One deployable host: a single ASP.NET Core app serves both the minimal-API endpoints and
  the Blazor UI, so the whole thing ships as one container and runs front-to-back.

## Verified stack (7-10 Jul 2026 - re-verify at build time, this area moves fast)

- Microsoft.Agents.AI 1.13.0 (stable), Microsoft.Agents.AI.Foundry 1.13.0-preview
- Azure.AI.Projects 2.1.0-beta.4, Azure.Identity 1.21.0
- Microsoft.Extensions.AI 10.7.0 (the IChatClient / IEmbeddingGenerator seam)
- Azure.AI.OpenAI 2.9.0-beta.1 (Azure OpenAI-compatible inference for chat + embeddings)
- Azure.AI.ContentSafety 1.0.0
- pgvector 0.6.0 (HNSW), Npgsql 10.0.3, Pgvector 0.3.2

## Architecture

### Solution layout

- `FcaHandbookAssistant.Core` - domain, RAG pipeline, agent, guardrails, retrieval, and the
  AI-service seam. No cloud SDK types leak to consumers; everything is behind interfaces.
- `FcaHandbookAssistant.Api` (evolved from the skeleton) - the single deployable ASP.NET
  Core host: minimal-API endpoints plus the Blazor (interactive-server) UI, referencing
  Core. One container, front-to-back.
- `FcaHandbookAssistant.Ingestion` - console tool: fetch curated provisions, chunk, embed,
  and upsert into pgvector (and, in the cloud, Azure AI Search).
- `FcaHandbookAssistant.Tests` - unit tests.
- `FcaHandbookAssistant.Evals` - the regulated-behaviour eval harness (its own project so it
  can gate CI and also run live against the real model).

### The AI-service seam

Everything depends on Microsoft.Extensions.AI abstractions - `IChatClient` and
`IEmbeddingGenerator`. Two implementations sit behind each:

- Real - Azure AI Foundry project inference for chat and embeddings; the tool-calling agent
  via the Agent Framework Foundry provider (`AIProjectClient(...).AsAIAgent(...)`,
  code-first Responses agent). Exact API surface pinned at build time.
- Fake - deterministic in-repo `FakeChatClient` / `FakeEmbeddingGenerator` that return
  canned grounded answers, citations, and stable embeddings keyed off the prompt. This is
  what makes unit tests and CI evals free and deterministic; the real model is only used in
  the deploy/capture run. Because the Foundry agent still surfaces through
  Microsoft.Extensions.AI, the fake path exercises the same guardrail and eval code.

### Data (local Postgres + pgvector)

- `provisions` - `reference` (e.g. `PRIN 2.1.1`), `sourcebook`, `title`, `url`,
  `chunk_text`, `embedding vector(1536)` (text-embedding-3-small), `content_hash`; HNSW
  index on the embedding.
- `answer_audit` - the per-answer traceability record: question (PII-redacted), retrieved
  references and scores (jsonb), cited references, refused flag and reason, model, prompt
  version, input/output Content-Safety verdicts, latency, and token counts.

Schema is applied by an idempotent migration the app and ingestion tool run on start.

### RAG and grounding-or-refuse (the star)

1. Retrieve the top-k provisions for the question by vector similarity.
2. If the best similarity is below a threshold, refuse: "not found in the provisions I
   have". No model call is needed to refuse on weak retrieval.
3. Otherwise prompt the model with only the retrieved provisions, instructed to answer
   solely from them and cite each claim with a provision reference, returning a structured
   answer `{ Answer, Citations[]{ Reference, Url, Quote }, Refused, Reason }`.
4. Deterministic post-check: every cited reference must be one of the retrieved provisions.
   A citation that is not is treated as a hallucination - the answer is refused/flagged.
   This check is pure logic, unit-testable without a model, and is exactly what the evals
   assert.
5. Persist the audit record regardless of outcome.

### The tool-calling agent

Three function tools registered with the Foundry Responses agent, sharing the retriever and
store:

- `lookup_rule(reference)` - fetch a specific provision by reference.
- `find_related_rules(topic)` - semantic search for cross-referenced provisions.
- `check_scenario(description)` - map a described situation to applicable sections; refuses
  on a weak match.

The same grounding-or-refuse discipline and audit logging apply to agent answers.

### Guardrails around it

- Content Safety on input and output - `IContentSafetyClient` with an Azure implementation
  and a local pass-through fake.
- PII-aware handling - redact user scenario text before logging or persisting, behind an
  interface (a lightweight local detector for dev/CI; Azure AI Language in the cloud).
- Audit log - every answer persisted to `answer_audit` for traceability.

### Eval harness (citation accuracy + refusal-when-unsure)

A gold-set file of cases `{ question, expect: answer|refuse, expectedCitations?, mustNotCite? }`.
Metrics: citation accuracy (cited references versus expected) and refusal correctness
(out-of-scope questions must refuse). The harness runs in two modes:

- CI, against the fake - deterministic; proves the guardrail logic catches a planted
  hallucination or a regression and fails the build. Free, runs on every PR.
- Live, env-gated - runs against the real Foundry model in the deploy session and emits a
  markdown eval report (citation accuracy, refusal rate, per-case detail).

Written test-first: the harness and the guardrail logic are a testable spec, so they come
before the wiring that satisfies them.

### Frontend

A thin Blazor (interactive-server) UI in the same host: ask a compliance question, see the
grounded answer with citations linked back to `handbook.fca.org.uk`, or the refusal, plus
an audit panel showing what was retrieved versus what was cited.

### IaC, CI/CD, observability

- Terraform (`infra/`, azure-housekeeping remote state backend): a session-scoped resource
  group, an Azure AI Foundry account + project with chat and embedding model deployments,
  Azure AI Search (free tier) for the cloud retrieval variant, Content Safety, Log Analytics
  + Application Insights, a Container Apps environment and app (min replicas 0, scale to
  zero), and Key Vault with a managed identity for the app. Cloud Postgres is kept optional
  to save cost; the cloud demo retrieves from AI Search, the local demo from pgvector.
- CI/CD (GitHub Actions): the existing dotnet (format/build/test), terraform
  (fmt/validate/tflint/trivy), CodeQL, and gitleaks stay green. Add an evals job (fake,
  gates PRs on regression) and a manual deploy workflow (OIDC: apply infra, build and push
  the container, deploy to Container Apps, run live evals, publish the eval report), plus a
  documented/scripted `terraform destroy`.
- Observability: OpenTelemetry exported to Azure Monitor; custom metrics for token cost,
  latency, refusal rate, and citation counts; a workbook/dashboard JSON committed in-repo.

### Handbook content handling

The ingestion tool politely fetches a curated list of provision URLs from
`handbook.fca.org.uk` (rate-limited, identified user agent, robots-respecting). The repo
stores only a manifest (references and URLs) plus code - never the wholesale Handbook text.
Chunks and embeddings live in the database. Every citation links back to
`handbook.fca.org.uk`.

## Sequencing (this session)

1. M1 - local core, test-first, zero cloud: projects, schema, RAG, grounding-or-refuse,
   agent + tools, guardrails, ingestion tool, eval harness (green against the fake), Blazor
   UI. Committed in logical chunks; format/build/test green.
2. M2 - real Azure: write and validate infra and CI/CD, then apply, ingest, run live evals,
   exercise a real cited answer and a real refusal, capture (screens, dashboard, eval
   report, cost), then destroy.
3. Docs: architecture diagram and the AI-DevOps write-up. CV/LinkedIn capability lines are
   delivered separately and kept out of the repo.

## Non-negotiables

- Cost: consumption/serverless only; deploy-capture-destroy each session; nothing runs
  overnight; watch the Sponsorship balance (no Cost Management budget on that offer).
- Provenance: commits authored Daniel Grimes <dan@dbhq.uk>, conventional commits, British
  English, plain hyphens, and no AI attribution anywhere in history or repo content.
- Secrets: no secrets in the repo; local config via gitignored `.env`/user-secrets; cloud
  via `az login`/OIDC and Key Vault; the subscription id never committed (gitleaks guard).

## Out of scope (for now)

- Wholesale ingestion across all sourcebooks.
- Cloud Postgres/pgvector (optional; AI Search covers the cloud retrieval story).
- A versioned server-managed Foundry Agent (noted as an alternative, not built).
