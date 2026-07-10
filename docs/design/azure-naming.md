# Azure resource naming convention

All Azure resources follow the Microsoft Cloud Adoption Framework (CAF) naming guidance:

- Define your naming convention: <https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming>
- Abbreviation recommendations: <https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations>

## Pattern

```
<resource-type-abbreviation>-<workload>-<environment>-<region>-<instance>
```

- **Delimiter:** hyphen (`-`) where the resource allows it. Resources that forbid hyphens
  (storage accounts `st`, container registries `cr`) use a concatenated lowercase form.
- **Components:** `workload = fca`, `environment = dev`, `region = uks` (UK South, from the
  location), `instance = 01`. These are Terraform variables with defaults (see `infra/variables.tf`).
- **Global uniqueness:** resources whose name must be unique across all of Azure (their name is a
  public DNS/subdomain) get a short, stable suffix derived from the subscription id
  (`substr(md5(subscription_id), 0, 5)`), and drop the instance number. This keeps names stable
  across the deploy-capture-destroy cycle while avoiding global collisions.

The convention is implemented once in `infra/locals.tf` (`local.names.*`); every resource
references it, so the whole estate is consistent by construction.

## Resources in this project

| Resource | Abbr | Scope | Example name |
| --- | --- | --- | --- |
| Resource group | `rg` | Subscription | `rg-fca-dev-uks-01` |
| Foundry account (AIServices) | `aif` | Global (subdomain) | `aif-fca-dev-uks-<suffix>` |
| Foundry project | `proj` | Account | `proj-fca-dev-uks-01` |
| Azure AI Search | `srch` | Global | `srch-fca-dev-uks-<suffix>` |
| Content Safety | `cs` | Global (subdomain) | `cs-fca-dev-uks-<suffix>` |
| Log Analytics workspace | `log` | Resource group | `log-fca-dev-uks-01` |
| Application Insights | `appi` | Resource group | `appi-fca-dev-uks-01` |
| Container Apps environment | `cae` | Resource group | `cae-fca-dev-uks-01` |
| Container App | `ca` | Resource group | `ca-fca-dev-uks-01` |
| Managed identity (user-assigned) | `id` | Resource group | `id-fca-dev-uks-01` |
| Key Vault | `kv` | Global | `kv-fca-dev-<suffix>` |
| Container Registry | `cr` | Global | `crfcadevuks<suffix>` |
| Postgres flexible server | `pgsql` | Global | `pgsql-fca-dev-uks-<suffix>` |
| Storage account | `st` | Global | `stfcadevuks<suffix>` |

Length limits are respected: Key Vault (<= 24), storage account (<= 24, lowercase alphanumeric),
container registry (<= 50, alphanumeric only). Container Registry, Postgres, and Storage are only
provisioned if the relevant option is enabled.
