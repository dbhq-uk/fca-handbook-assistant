# fca-handbook-assistant

A full-stack .NET reference implementation of **regulated-grade AI on Azure**: grounded compliance question-answering over the public [FCA Handbook](https://www.handbook.fca.org.uk/), with a tool-calling agent for looking up and cross-referencing specific provisions.

It is an AI-DevOps reference project - it evidences production .NET, the Azure AI service layer, and LLMOps (evals, cost observability, CI/CD) in a regulated financial-services setting.

## The distinctive angle - regulated-grade guardrails

The star is exactly what regulated AI needs, not just a chatbot:

- **Strict grounding** - every answer cites a specific handbook provision (SYSC, COBS, SUP, PRIN, ...) or the assistant refuses. No hallucinated regulations.
- **Per-answer source audit log** - which provisions were retrieved and cited, persisted for traceability.
- **Content Safety** on input and output; PII-aware handling of user-entered scenarios.
- **Evals that test the regulated behaviour** - citation accuracy and refusal-when-unsure, not just answer quality.

## Stack

- **Microsoft Agent Framework** (the supported successor to Semantic Kernel) with the code-first Responses Agent pattern
- **ASP.NET Core** (.NET 10) minimal API + a thin **Blazor** UI
- Retrieval over **Azure AI Search**, with a **pgvector** variant to evidence both
- **LLMOps**: Terraform IaC, GitHub Actions CI/CD, an xUnit eval project, App Insights observability
- Hosted on **Container Apps** (scale-to-zero)

## Structure

- `src/` - the ASP.NET Core API and Blazor UI
- `tests/` - unit and eval projects
- `infra/` - Terraform for the Azure resources

## Status

Scaffold - a buildable skeleton (API + test project, infra placeholder). The build is specified in the handoff brief that seeds it.

## Handbook content

The FCA Handbook is publicly published but Crown/FCA copyright. Cite and link back to `handbook.fca.org.uk`; store chunks/embeddings for retrieval; do not wholesale redistribute the Handbook text in this repo.

## Cost discipline

Shares one Azure credit with the sibling repos. Consumption/serverless tiers, remote state in the `azure-housekeeping` backend, and deploy-capture-destroy each session.

## Licence

MIT - see [LICENSE](LICENSE).
