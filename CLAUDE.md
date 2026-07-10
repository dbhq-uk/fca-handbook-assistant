# fca-handbook-assistant - working notes

Public proof-of-worth repo: a full-stack .NET reference implementation of regulated-grade AI on Azure - grounded compliance Q&A over the public FCA Handbook, plus a tool-calling agent. The AI-DevOps interview centrepiece.

## What matters here

- **Use Microsoft Agent Framework, not Semantic Kernel** (SK is superseded and maintenance-only). Re-verify the current package versions and the `AsAIAgent` / Foundry provider API at build time - this area moves fast.
- **The guardrails are the point.** Grounded citations or refusal (no hallucinated rules), per-answer source audit log, Content Safety, and evals that test citation accuracy + refusal-when-unsure. Build these early - they are a testable spec.
- **LLMOps is not optional polish.** Evals in CI, cost/latency in App Insights, IaC + CI/CD. That layer is what makes this hireable.
- **Cost safety:** consumption/serverless only, remote state in the `azure-housekeeping` backend, deploy-capture-destroy each session.

## Corpus handling

The FCA Handbook is publicly published but Crown/FCA copyright. Cite and link back to `handbook.fca.org.uk`, store chunks/embeddings for retrieval, and do not wholesale redistribute the corpus in the repo. Ingest politely.

## Conventions

- .NET: keep `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` green. Terraform in `infra/`: keep `fmt`/`validate`/`tflint`/`trivy` green.
- No secrets in the repo (`appsettings.*.local.json` and `.env` are gitignored; `terraform.tfvars` too). Authenticate via `az login` / OIDC and Key Vault.
- British English, plain hyphens (never em/en dashes), conventional-commit messages.

## Provenance

- Commits authored **Daniel Grimes <dan@dbhq.uk>** (repo-local git identity).
- No AI attribution anywhere: no `Co-Authored-By` trailer, no "Generated with" lines, no mention of AI assistants in commits, PRs, README or repo content. This is proof-of-worth - the history must read as Dan's work.
