# fca-handbook-assistant

A full-stack .NET reference implementation of **regulated-grade AI on Azure**: grounded compliance question-answering over the public [FCA Handbook](https://www.handbook.fca.org.uk/), with a tool-calling agent for looking up and cross-referencing specific provisions.

It is an AI-DevOps reference project - it evidences production .NET, the Azure AI service layer, and LLMOps (evals, cost observability, CI/CD) in a regulated financial-services setting.

## The distinctive angle - regulated-grade guardrails

The star is exactly what regulated AI needs, not just a chatbot:

- **Strict grounding** - every answer cites a specific handbook provision (PRIN, SYSC, COBS, SUP, ...) or the assistant refuses. A deterministic policy validates each citation against what was actually retrieved, so a hallucinated rule is dropped or the answer is refused.
- **Per-answer source audit log** - what was retrieved, what was cited, the model and prompt version, safety verdicts, latency and token counts - persisted for traceability.
- **Content Safety** on input and output, and PII-aware handling of user-entered scenarios.
- **Evals that test the regulated behaviour** - a CI gate measuring citation accuracy and refusal-when-unsure, failing the build on regression, plus an env-gated live run against the real model.

## Architecture

```
Blazor UI ─▶ Minimal API ─▶ GroundedAnswerService
                               │  content safety ▶ PII redact ▶ embed ▶ retrieve
                               │  ▶ refuse if weak ▶ grounded generation ▶ grounding policy
                               │  ▶ content safety ▶ audit
                               ├─▶ IEmbeddingGenerator / IChatClient  (local fake | Azure OpenAI)
                               ├─▶ IRetriever  (pgvector | Azure AI Search)
                               └─▶ IAuditSink  (Postgres)
Tool-calling agent ─▶ Foundry Responses agent (lookup_rule, find_related_rules, check_scenario)
```

Everything the model touches is behind `Microsoft.Extensions.AI` interfaces, so the same code runs
against a deterministic local implementation (dev, tests, CI - no cloud) or Azure AI Foundry. See
[docs/design](docs/design) for the design and [docs/design/azure-naming.md](docs/design/azure-naming.md)
for the resource naming convention.

## Stack

- **Microsoft Agent Framework** (Foundry provider) with the code-first `AsAIAgent` Responses agent - the supported successor to Semantic Kernel
- **ASP.NET Core** (.NET 10) minimal API + a thin **Blazor** UI, in one deployable container
- Retrieval on **pgvector** (primary) with an **Azure AI Search** variant behind the same interface
- **LLMOps**: Terraform IaC, GitHub Actions CI/CD, an xUnit eval project, Application Insights via OpenTelemetry
- Hosted on **Container Apps** (scale-to-zero)

## Structure

- `src/FcaHandbookAssistant.Core` - domain, RAG, agent, guardrails, retrieval, the AI-service seam
- `src/FcaHandbookAssistant.Api` - the deployable host (minimal API + Blazor UI)
- `src/FcaHandbookAssistant.Ingestion` - console tool that loads provisions into pgvector
- `tests/FcaHandbookAssistant.Tests` - unit and integration tests
- `tests/FcaHandbookAssistant.Evals` - the regulated-behaviour eval harness
- `infra/` - Terraform for the Azure resources
- `scripts/` - the headless-browser Handbook fetcher

## Run it locally (no cloud)

Prerequisites: the .NET 10 SDK and PostgreSQL with the `pgvector` extension.

```bash
# 1. A database the app can reach (pgvector enabled), then:
export FCA_DB="Host=localhost;Database=fca_handbook;Username=fca;Password=..."

# 2. (optional) fetch the real Handbook text into a gitignored cache; otherwise
#    ingestion falls back to short summaries for offline use.
python3 scripts/fetch_handbook.py
export FCA_TEXT_FILE="$PWD/artifacts/handbook-text.json"

# 3. Ingest the curated provisions (local deterministic embeddings, no cloud).
dotnet run --project src/FcaHandbookAssistant.Ingestion -- --dry-run

# 4. Run the app and open the Blazor UI, or call the API:
dotnet run --project src/FcaHandbookAssistant.Api
curl -X POST localhost:5xxx/api/ask -H 'content-type: application/json' \
  -d '{"question":"What must a firm obtain to assess suitability?"}'
```

`dotnet test` runs the unit tests and the eval CI gate. Database-backed tests are skipped unless
`FCA_TEST_DB` is set to a connection string.

Set `Ai:Mode=Azure` (with the Foundry endpoint and deployments) to run against real models.

### Embeddings

Retrieval quality depends on the embedding model. Three providers are supported behind one
interface (set `Ai:Embeddings:Provider`, and `EMBEDDINGS` for ingestion):

- **Local** (default) - deterministic hashing-trick embeddings; zero dependencies, for offline
  determinism and CI. Keyword-level, not semantic.
- **Ollama** - real semantic embeddings from a local [Ollama](https://ollama.com) model
  (`all-minilm`, 384 dimensions) via its OpenAI-compatible endpoint. No cloud; used by the
  `semantic-evals` CI job. Set `Ai:Embeddings:Dimensions=384` and a retrieval floor around `0.35`.
- **Azure** - Azure OpenAI / Foundry (`text-embedding-3-small`, 1536 dimensions).

The store's vector dimension must match the provider; ingestion sets it, so re-ingest into a fresh
table when switching provider.

## Handbook content

The FCA Handbook is publicly published but Crown/FCA copyright. The Handbook is a JavaScript
single-page app, so `scripts/fetch_handbook.py` renders each curated provision with a headless
browser and writes the real text to a **gitignored** cache; ingestion stores it for retrieval and
every answer links back to the authoritative provision on `handbook.fca.org.uk`. The repo never
commits the Handbook text - only a manifest of references, URLs, and short summaries used as an
offline fallback.

## Deploy (Azure)

`infra/` provisions Azure AI Foundry (chat + embedding deployments), Postgres Flexible with
pgvector, Container Apps (scale-to-zero), Azure AI Search, Content Safety, Application Insights, and
Key Vault with a managed identity. The app authenticates to Azure services with its managed
identity. State lives in the shared `azure-housekeeping` backend.

Cost discipline: consumption/serverless tiers only, and deploy-capture-destroy each session -
nothing runs overnight.

## Licence

MIT - see [LICENSE](LICENSE).
