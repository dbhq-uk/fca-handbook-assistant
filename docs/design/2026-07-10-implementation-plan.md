# FCA Handbook Assistant - implementation plan

> **For agentic workers:** implement task-by-task, test-first. Steps use checkbox
> (`- [ ]`) syntax for tracking. Each task ends green (format/build/test) and is committed.

**Goal:** Build grounded, cited FCA-Handbook Q&A (RAG) plus a tool-calling agent with
regulated-grade guardrails, a Blazor UI, an eval harness, IaC, CI/CD, and observability -
then deploy to Azure, capture evidence, and destroy.

**Architecture:** A `Core` library holds the domain, retrieval, grounding-or-refuse logic,
guardrails, and the agent, all behind Microsoft.Extensions.AI interfaces. A single ASP.NET
Core host serves the minimal API and Blazor UI. An ingestion console tool loads a curated
Handbook subset into pgvector. An eval project gates CI against a deterministic fake and
runs live against Azure AI Foundry in the deploy session.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI 10.7, Microsoft.Agents.AI(.Foundry) 1.13,
Azure.AI.Projects, Azure.AI.OpenAI, Azure.AI.ContentSafety, Npgsql 10 + Pgvector, xUnit,
Blazor, Terraform (azurerm), GitHub Actions, Application Insights.

## Global Constraints

- .NET 10; keep `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` green.
- Terraform in `infra/`: keep `fmt`/`validate`/`tflint`/`trivy` green.
- No secrets in the repo; local config via gitignored `.env`/user-secrets; the subscription
  id never committed (gitleaks guard). Auth via `az login`/OIDC and Key Vault.
- British English, plain hyphens (never em/en dashes), conventional-commit messages.
- Commits authored Daniel Grimes <dan@dbhq.uk>; no AI attribution anywhere.
- Consumption/serverless only; deploy-capture-destroy; nothing runs overnight.
- Re-verify Azure/Foundry SDK + Terraform resource surface at build time; pin exact versions.
- Local DB: `Host=localhost;Database=fca_handbook;Username=fca` (pgvector 0.6.0 present);
  password lives only in gitignored `.env`/user-secrets.

---

## Phase 0 - solution scaffolding

### Task 0.1: Add projects and central package management

**Files:** Create `Directory.Packages.props`; create `src/FcaHandbookAssistant.Core`,
`src/FcaHandbookAssistant.Ingestion`, `tests/FcaHandbookAssistant.Evals`,
`tests/FcaHandbookAssistant.TestSupport`; modify `FcaHandbookAssistant.slnx`,
`FcaHandbookAssistant.Api.csproj`.

- [ ] Add central package management (`Directory.Packages.props`) with the verified versions.
- [ ] `dotnet new classlib` for Core (net10.0); `dotnet new console` for Ingestion; two
  `dotnet new xunit`/classlib for Evals and TestSupport.
- [ ] Api references Core; Ingestion references Core; TestSupport references Core; Tests and
  Evals reference Core + TestSupport.
- [ ] Add all to `FcaHandbookAssistant.slnx`.
- [ ] `dotnet build -c Release` green; `dotnet format --verify-no-changes` clean.
- [ ] Commit: `chore: add Core, Ingestion, Evals, TestSupport projects`.

---

## Phase 1 - domain and the deterministic grounding policy (pure, TDD)

### Task 1.1: Domain records

**Files:** Create `src/FcaHandbookAssistant.Core/Domain/{Provision,RetrievedProvision,Citation,GroundedAnswer,AuditRecord}.cs`.

**Produces:**
- `record Provision(string Reference, string Sourcebook, string Title, string Url, string ChunkText, string ContentHash)`
- `record RetrievedProvision(Provision Provision, double Score)`
- `record Citation(string Reference, string Url, string Quote)`
- `record GroundedAnswer(string Text, IReadOnlyList<Citation> Citations, bool Refused, string? Reason)`
- `record AuditRecord(...)` (question, redactedQuestion, retrieved refs+scores, cited refs,
  refused, reason, model, promptVersion, inputSafety, outputSafety, latencyMs, promptTokens,
  completionTokens, timestamp)

- [ ] Write the records. No behaviour, so no test; they are exercised by later tasks.
- [ ] Build green. Commit: `feat(core): add domain records`.

### Task 1.2: GroundingPolicy - citation validation and refusal (TDD)

**Files:** Create `src/FcaHandbookAssistant.Core/Grounding/GroundingPolicy.cs`; Test
`tests/FcaHandbookAssistant.Tests/Grounding/GroundingPolicyTests.cs`.

**Interfaces produced:**
- `static GroundedAnswer Apply(IReadOnlyList<RetrievedProvision> retrieved, GroundedAnswer modelAnswer, double retrievalFloor)`
  - If `retrieved` empty or top score < `retrievalFloor` -> refuse ("not found in the
    provisions available").
  - Else drop any citation whose `Reference` is not among `retrieved` references; if no
    citations survive -> refuse ("could not ground an answer in the retrieved provisions").
  - Else return the answer with only surviving citations.

- [ ] **Step 1 - failing tests** (write these first):

```csharp
public class GroundingPolicyTests
{
    static RetrievedProvision R(string reference, double score) =>
        new(new Provision(reference, "PRIN", "t", "https://handbook.fca.org.uk/x", "body", "h"), score);
    static GroundedAnswer Answer(params string[] citedRefs) =>
        new("text", citedRefs.Select(r => new Citation(r, "u", "q")).ToArray(), false, null);

    [Fact]
    public void Refuses_when_nothing_retrieved()
    {
        var result = GroundingPolicy.Apply(Array.Empty<RetrievedProvision>(), Answer("PRIN 2.1.1"), 0.5);
        Assert.True(result.Refused);
    }

    [Fact]
    public void Refuses_when_top_score_below_floor()
    {
        var result = GroundingPolicy.Apply(new[] { R("PRIN 2.1.1", 0.10) }, Answer("PRIN 2.1.1"), 0.5);
        Assert.True(result.Refused);
    }

    [Fact]
    public void Drops_hallucinated_citation_not_in_retrieved_set()
    {
        var result = GroundingPolicy.Apply(new[] { R("PRIN 2.1.1", 0.9) }, Answer("SYSC 9.9.9"), 0.5);
        Assert.True(result.Refused); // the only citation was fabricated -> none survive
    }

    [Fact]
    public void Keeps_only_grounded_citations()
    {
        var result = GroundingPolicy.Apply(new[] { R("PRIN 2.1.1", 0.9) },
            Answer("PRIN 2.1.1", "SYSC 9.9.9"), 0.5);
        Assert.False(result.Refused);
        Assert.Single(result.Citations);
        Assert.Equal("PRIN 2.1.1", result.Citations[0].Reference);
    }
}
```

- [ ] **Step 2** - run, verify fail (`GroundingPolicy` not defined).
- [ ] **Step 3** - implement `GroundingPolicy.Apply` (normalise references case/space-insensitively).
- [ ] **Step 4** - `dotnet test` green.
- [ ] **Step 5** - commit: `feat(core): grounding policy - validate citations or refuse`.

### Task 1.3: PII redaction (TDD)

**Files:** Create `src/FcaHandbookAssistant.Core/Guardrails/{IPiiRedactor,RegexPiiRedactor}.cs`;
Test `tests/FcaHandbookAssistant.Tests/Guardrails/RegexPiiRedactorTests.cs`.

**Interfaces produced:**
- `interface IPiiRedactor { RedactionResult Redact(string text); }`
- `record RedactionResult(string Redacted, IReadOnlyList<string> Kinds)`

- [ ] Failing tests: email, UK phone, and what looks like an account/sort-code number get
  replaced with `[REDACTED:EMAIL]` etc.; plain text passes through unchanged.
- [ ] Implement `RegexPiiRedactor` (email, UK phone, 8+ digit runs, NI number pattern).
- [ ] `dotnet test` green. Commit: `feat(core): regex PII redactor for user input`.

---

## Phase 2 - the AI seam and deterministic fakes

### Task 2.1: Fakes in TestSupport (deterministic IChatClient + IEmbeddingGenerator)

**Files:** Create `tests/FcaHandbookAssistant.TestSupport/{FakeChatClient,FakeEmbeddingGenerator,FakeContentSafety}.cs`.

**Interfaces produced:**
- `FakeEmbeddingGenerator` - deterministic vector from a hash of the text (stable across runs,
  same dimensionality as prod, e.g. 1536), so cosine similarity is reproducible.
- `FakeChatClient` - configurable: given a prompt, returns a scripted `GroundedAnswer` JSON
  (used to simulate both a good grounded answer and a planted hallucination for eval tests).
- `FakeContentSafety` - returns safe by default; configurable to flag a phrase.

- [ ] Implement all three against Microsoft.Extensions.AI interfaces. No cloud calls.
- [ ] A smoke test in Tests: same text -> identical embedding; different text -> different.
- [ ] `dotnet test` green. Commit: `test: deterministic fake chat/embedding/safety clients`.

### Task 2.2: Content Safety abstraction + Azure impl (interface TDD, Azure impl verify-at-build)

**Files:** Create `src/FcaHandbookAssistant.Core/Guardrails/{IContentSafetyClient,SafetyVerdict}.cs`;
`src/FcaHandbookAssistant.Core/Azure/AzureContentSafetyClient.cs`.

**Interfaces produced:**
- `interface IContentSafetyClient { Task<SafetyVerdict> InspectAsync(string text, CancellationToken ct); }`
- `record SafetyVerdict(bool Blocked, string? Category, string Raw)`

- [ ] Define the interface + record; the fake already implements it.
- [ ] Implement `AzureContentSafetyClient` using `Azure.AI.ContentSafety` 1.0.0 - **verify the
  current `AnalyzeText` request/response surface before writing** (`az`/docs), map severities
  to `Blocked`.
- [ ] Build green (no live call in CI). Commit: `feat(core): content safety abstraction + azure client`.

---

## Phase 3 - retrieval on pgvector

### Task 3.1: Schema bootstrap

**Files:** Create `src/FcaHandbookAssistant.Core/Data/schema.sql`,
`src/FcaHandbookAssistant.Core/Data/SchemaBootstrapper.cs`.

- [ ] `schema.sql`: `CREATE EXTENSION IF NOT EXISTS vector;` `provisions` table (columns per
  design; `embedding vector(1536)`); HNSW index; `answer_audit` table.
- [ ] `SchemaBootstrapper.EnsureAsync(NpgsqlDataSource)` runs it idempotently.
- [ ] Integration test (gated on a reachable local DB) that ensures the schema applies twice
  without error. Skips cleanly if the DB is unreachable (so CI without a DB stays green).
- [ ] Commit: `feat(core): pgvector schema bootstrap`.

### Task 3.2: PgVectorProvisionStore + PgVectorRetriever (integration TDD)

**Files:** Create `src/FcaHandbookAssistant.Core/Abstractions/{IProvisionStore,IRetriever}.cs`;
`src/FcaHandbookAssistant.Core/Retrieval/PgVector{ProvisionStore,Retriever}.cs`;
Test `tests/FcaHandbookAssistant.Tests/Retrieval/PgVectorRetrieverTests.cs`.

**Interfaces produced:**
- `interface IProvisionStore { Task UpsertAsync(Provision p, ReadOnlyMemory<float> embedding, CancellationToken ct); Task<Provision?> GetByReferenceAsync(string reference, CancellationToken ct); }`
- `interface IRetriever { Task<IReadOnlyList<RetrievedProvision>> RetrieveAsync(ReadOnlyMemory<float> queryEmbedding, int k, CancellationToken ct); }`

- [ ] Integration tests (DB-gated, using `FakeEmbeddingGenerator` for stable vectors): upsert
  three provisions, retrieve top-2 for a query embedding, assert ordering by cosine distance;
  `GetByReferenceAsync` round-trips. Upsert is idempotent on `reference`.
- [ ] Implement store + retriever with Npgsql 10 + Pgvector (`<=>` cosine operator; map
  distance to a 0-1 score).
- [ ] `dotnet test` green against the local DB. Commit: `feat(core): pgvector store + retriever`.

### Task 3.3: AzureAiSearchRetriever (cloud variant, verify-at-build)

**Files:** Create `src/FcaHandbookAssistant.Core/Retrieval/AzureAiSearchRetriever.cs`.

- [ ] Implement `IRetriever` (and an index upsert path) over `Azure.Search.Documents` -
  **verify the current vector-query surface before writing**. Not run in CI; exercised in the
  deploy session.
- [ ] Build green. Commit: `feat(core): azure ai search retriever variant`.

---

## Phase 4 - the grounded answer service (orchestration, TDD)

### Task 4.1: Prompt template + version

**Files:** Create `src/FcaHandbookAssistant.Core/Ai/PromptTemplates.cs`.

- [ ] System prompt: answer only from the provided provisions; cite each claim with its
  reference; if the provisions do not answer, refuse. `const string PromptVersion = "2026-07-10.1"`.
- [ ] Structured-output schema for `GroundedAnswer`.
- [ ] Commit: `feat(core): grounding prompt template + version`.

### Task 4.2: GroundedAnswerService (TDD against fakes)

**Files:** Create `src/FcaHandbookAssistant.Core/Grounding/{IGroundedAnswerService,GroundedAnswerService}.cs`;
Test `tests/FcaHandbookAssistant.Tests/Grounding/GroundedAnswerServiceTests.cs`.

**Interfaces produced:**
- `interface IGroundedAnswerService { Task<GroundedAnswer> AskAsync(string question, CancellationToken ct); }`
- Constructor consumes: `IChatClient`, `IEmbeddingGenerator`, `IRetriever`,
  `IContentSafetyClient`, `IPiiRedactor`, `IAuditSink`, options (k, retrievalFloor).

Flow: input safety -> PII redact -> embed -> retrieve -> `GroundingPolicy` floor check ->
if ok, chat with structured output -> `GroundingPolicy.Apply` -> output safety -> write audit
-> return.

- [ ] Failing tests (fakes wired): (a) a normal question yields a grounded answer citing a
  retrieved reference; (b) an out-of-scope question (fake retrieval below floor) refuses; (c)
  a fake model that cites a non-retrieved reference is refused; (d) an audit record is written
  in every case; (e) blocked input safety short-circuits to a safe refusal.
- [ ] Implement the service. `dotnet test` green.
- [ ] Commit: `feat(core): grounded answer service with guardrails + audit`.

### Task 4.3: IAuditSink - Postgres + in-memory

**Files:** Create `src/FcaHandbookAssistant.Core/Abstractions/IAuditSink.cs`,
`src/FcaHandbookAssistant.Core/Data/PostgresAuditSink.cs`,
`tests/FcaHandbookAssistant.TestSupport/InMemoryAuditSink.cs`.

- [ ] `interface IAuditSink { Task WriteAsync(AuditRecord r, CancellationToken ct); }`.
- [ ] In-memory fake (used by Task 4.2 tests). Postgres sink writes to `answer_audit`
  (DB-gated integration test round-trips one record).
- [ ] `dotnet test` green. Commit: `feat(core): answer audit sink (postgres + in-memory)`.

---

## Phase 5 - the tool-calling agent

### Task 5.1: Handbook tools

**Files:** Create `src/FcaHandbookAssistant.Core/Agent/HandbookTools.cs`; Test
`tests/FcaHandbookAssistant.Tests/Agent/HandbookToolsTests.cs`.

**Interfaces produced:** methods surfaced as `AIFunction`s:
- `Task<string> LookupRuleAsync(string reference)` - via `IProvisionStore.GetByReferenceAsync`.
- `Task<string> FindRelatedRulesAsync(string topic)` - via embed + `IRetriever`.
- `Task<string> CheckScenarioAsync(string description)` - embed + retrieve; refuse on weak match.

- [ ] TDD each against fakes/local DB: known reference returns the provision; unknown returns
  a "not found" payload; weak scenario match returns a refusal payload.
- [ ] `dotnet test` green. Commit: `feat(core): handbook agent tools`.

### Task 5.2: Foundry agent factory (verify-at-build)

**Files:** Create `src/FcaHandbookAssistant.Core/Agent/{IHandbookAgent,FoundryHandbookAgent}.cs`;
`tests/FcaHandbookAssistant.TestSupport/FakeHandbookAgent.cs`.

- [ ] `interface IHandbookAgent { Task<GroundedAnswer> AskAsync(string question, CancellationToken ct); }`.
- [ ] `FoundryHandbookAgent` builds the agent via `AIProjectClient(...).AsAIAgent(...)` with the
  three tools - **verify the current Agent Framework Foundry surface before writing**; run the
  agent, then apply `GroundingPolicy` + audit to its final answer.
- [ ] Fake agent for tests/UI-without-cloud.
- [ ] Build green. Commit: `feat(core): foundry tool-calling agent`.

---

## Phase 6 - ingestion tool

### Task 6.1: Manifest + fetch + chunk (TDD the pure parts)

**Files:** Create `src/FcaHandbookAssistant.Ingestion/data/manifest.json` (curated PRIN +
selected SYSC/COBS/SUP references with URLs); `src/FcaHandbookAssistant.Ingestion/{HandbookFetcher,ProvisionChunker}.cs`;
Test `tests/FcaHandbookAssistant.Tests/Ingestion/ProvisionChunkerTests.cs`.

- [ ] Manifest schema: `[{ reference, sourcebook, title, url }]`.
- [ ] `ProvisionChunker` (pure) - given provision HTML/text, produce clean chunk text +
  content hash. TDD with a small fixed HTML fixture.
- [ ] `HandbookFetcher` - polite HTTP (identified UA, delay, robots check). Not unit-run.
- [ ] `dotnet test` green. Commit: `feat(ingestion): manifest, fetcher, chunker`.

### Task 6.2: Ingest pipeline (embed + upsert)

**Files:** Modify `src/FcaHandbookAssistant.Ingestion/Program.cs`.

- [ ] Wire config -> data source -> `SchemaBootstrapper` -> for each manifest entry: fetch,
  chunk, embed (real or fake generator by config), `UpsertAsync`. `--dry-run` uses the fake.
- [ ] Run `--dry-run` end-to-end against the local DB to prove the pipeline (no network).
- [ ] Commit: `feat(ingestion): embed + upsert pipeline`.

---

## Phase 7 - API + Blazor UI

### Task 7.1: DI wiring + minimal API (config-selected fake vs Azure)

**Files:** Modify `src/FcaHandbookAssistant.Api/Program.cs`; create
`src/FcaHandbookAssistant.Api/Endpoints/AskEndpoints.cs`,
`src/FcaHandbookAssistant.Api/Configuration/AiOptions.cs`.

- [ ] Options: `Ai:Mode = Fake|Azure`. Fake mode wires TestSupport-equivalent fakes (moved to
  a shipping `Core/Ai/Local` namespace so the app can run cloud-free) + local pgvector.
- [ ] `POST /api/ask` -> `IGroundedAnswerService`; `POST /api/agent/ask` -> `IHandbookAgent`;
  `GET /health`.
- [ ] WebApplicationFactory test: `/api/ask` in Fake mode returns a grounded answer with a
  citation; an out-of-scope question returns a refusal. `dotnet test` green.
- [ ] Commit: `feat(api): ask + agent endpoints with fake/azure wiring`.

### Task 7.2: Blazor UI

**Files:** Add Razor components under `src/FcaHandbookAssistant.Api/Components/` (Ask page,
answer + citations, audit panel), enable interactive-server components in `Program.cs`.

- [ ] Ask page: question box -> calls the service -> shows answer with citation links to
  `handbook.fca.org.uk`, or the refusal, plus a retrieved-vs-cited audit panel.
- [ ] `dotnet build` green; manual smoke in Fake mode (screenshot for the write-up).
- [ ] Commit: `feat(web): blazor ask UI with citations + audit panel`.

---

## Phase 8 - eval harness

### Task 8.1: Gold set + metrics (TDD)

**Files:** Create `tests/FcaHandbookAssistant.Evals/data/gold-set.json`;
`tests/FcaHandbookAssistant.Evals/{EvalCase,EvalMetrics,EvalRunner}.cs`;
`tests/FcaHandbookAssistant.Evals/GuardrailEvals.cs`.

- [ ] Gold set: answerable cases with `expectedCitations`, and out-of-scope cases with
  `expect: refuse`; a planted-hallucination case the fake model "answers" with a fabricated
  reference.
- [ ] `EvalMetrics` (pure): citation accuracy (precision/recall vs expected) + refusal
  correctness. TDD with hand-built cases.
- [ ] `GuardrailEvals` runs the gold set through `GroundedAnswerService` in Fake mode and
  asserts thresholds (e.g. refusal correctness == 100%, no hallucinated citation survives).
  This is the CI gate.
- [ ] `dotnet test` green. Commit: `test(evals): gold set + citation/refusal metrics (CI gate)`.

### Task 8.2: Live eval runner + markdown report (env-gated)

**Files:** Create `tests/FcaHandbookAssistant.Evals/LiveEvals.cs`,
`tests/FcaHandbookAssistant.Evals/ReportWriter.cs`.

- [ ] `[Trait("Category","Live")]`/env-gated: run the gold set against Azure Foundry, write
  `docs/evals/eval-report.md` (accuracy, refusal rate, per-case). Skipped unless
  `RUN_LIVE_EVALS=1`.
- [ ] Build green. Commit: `test(evals): live eval runner + markdown report`.

---

## Phase 9 - IaC

### Task 9.1: Backend + AI Foundry + Search + Content Safety

**Files:** Modify `infra/versions.tf` (add backend), `infra/main.tf`; create
`infra/{ai_foundry,search,content_safety,observability,container_app,keyvault,outputs}.tf`,
`infra/locals.tf`.

- [ ] Add the `azurerm` backend block (rg `rg-dbhq-housekeeping`, sa `dbhqtfstateuks01`,
  container `tfstate`, key `fca-handbook-assistant.tfstate`).
- [ ] Name every resource via `local.names.*` from `infra/locals.tf` (CAF convention, see
  `docs/design/azure-naming.md`) - never hard-code names.
- [ ] Resources (verify current resource/module names before writing): AI Foundry account +
  project + chat + embedding deployments; AI Search (free); Content Safety; Log Analytics +
  App Insights; Container Apps env + app (min replicas 0); Key Vault + managed identity.
- [ ] `terraform fmt -check`, `init -backend=false`, `validate`, `tflint`, `trivy` green.
- [ ] Commit: `feat(infra): ai foundry, search, content safety, container app, observability`.

---

## Phase 10 - CI/CD + observability wiring

### Task 10.1: Evals gate + deploy workflow

**Files:** Modify `.github/workflows/dotnet.yml` (or add `evals.yml`); create
`.github/workflows/deploy.yml`.

- [ ] Add an evals step running the Fake-mode CI gate on every PR.
- [ ] `deploy.yml` (workflow_dispatch, OIDC): terraform apply, build+push container, deploy to
  Container Apps, run `RUN_LIVE_EVALS=1`, upload the eval report; documented destroy.
- [ ] Commit: `ci: eval gate + manual azure deploy workflow`.

### Task 10.2: Observability

**Files:** Modify `src/FcaHandbookAssistant.Api/Program.cs`; create
`infra/workbook.tf` or `docs/observability/workbook.json`.

- [ ] OpenTelemetry -> Azure Monitor; custom metrics: token cost, latency, refusal rate,
  citation count.
- [ ] Workbook JSON committed. Commit: `feat(obs): otel to app insights + workbook`.

---

## Phase 11 - deploy, capture, destroy

### Task 11.1: Provision + ingest + verify end-to-end

- [ ] `export TF_VAR_subscription_id=$(az account show --query id -o tsv)`; `terraform init`,
  `plan`, `apply` (session-scoped RG).
- [ ] Run ingestion against Azure (embeddings via Foundry) into AI Search (+ local pgvector).
- [ ] Exercise the deployed app: one real compliance question answered with a correct
  handbook citation; one out-of-scope question correctly refused.
- [ ] Run live evals; confirm the report.

### Task 11.2: Capture + destroy

- [ ] Capture: app screenshots, App Insights dashboard, eval report, Sponsorship cost view -
  into `docs/` (redacted).
- [ ] `terraform destroy`; confirm the RG is gone.
- [ ] Flip README status from "Scaffold" to a live/architecture summary; add the architecture
  diagram + AI-DevOps write-up.
- [ ] Commit: `docs: architecture, write-up, and captured evidence`.

---

## Self-review notes

- Spec coverage: grounding-or-refuse (1.2, 4.2), audit log (4.3), Content Safety (2.2), PII
  (1.3), agent tools (5.1-5.2), pgvector primary + AI Search variant (3.2-3.3), evals
  CI + live (8.1-8.2), IaC (9.1), CI/CD (10.1), observability (10.2), Blazor (7.2),
  ingestion + Handbook-content handling (6.1-6.2), deploy-capture-destroy (11).
- Fakes live in TestSupport for tests, and a shipping local variant in Core (Task 7.1) so the
  app runs cloud-free - keep the two in sync (single implementation, referenced by both).
- Verify-at-build tasks (2.2, 3.3, 5.2, 9.1) must re-check the SDK/resource surface first.
