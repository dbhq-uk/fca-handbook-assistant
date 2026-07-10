## What and why

<!-- One or two lines. Link any related issue. -->

## Checklist

- [ ] `dotnet build` and `dotnet test` pass; `dotnet format --verify-no-changes` clean
- [ ] `infra/` Terraform: `fmt`/`validate`/`tflint`/`trivy` green (if touched)
- [ ] Grounding holds - answers cite a provision or refuse; no hallucinated rules (if touching the RAG/agent path)
- [ ] No secrets, real subscription ids, or redistributed corpus committed
