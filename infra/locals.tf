# Cloud Adoption Framework resource naming, implemented once and referenced everywhere.
# Pattern: <abbreviation>-<workload>-<environment>-<region>-<instance>. See docs/design/azure-naming.md.
locals {
  region_abbreviations = {
    uksouth     = "uks"
    ukwest      = "ukw"
    northeurope = "neu"
    westeurope  = "weu"
    eastus      = "eus"
    eastus2     = "eus2"
    westus      = "wus"
    westus2     = "wus2"
  }

  region   = lookup(local.region_abbreviations, var.location, "unk")
  instance = "01"

  # Hyphenated base for most resources; compact lowercase form for those that forbid hyphens.
  base    = "${var.workload}-${var.environment}-${local.region}-${local.instance}"
  compact = lower("${var.workload}${var.environment}${local.region}")

  # Stable, globally-unique-ish suffix for global-scope resources (name is a public subdomain).
  suffix = substr(md5(var.subscription_id), 0, 5)

  names = {
    resource_group     = "rg-${local.base}"
    foundry_account    = "aif-${var.workload}-${var.environment}-${local.region}-${local.suffix}"
    foundry_project    = "proj-${local.base}"
    ai_search          = "srch-${var.workload}-${var.environment}-${local.region}-${local.suffix}"
    content_safety     = "cs-${var.workload}-${var.environment}-${local.region}-${local.suffix}"
    log_analytics      = "log-${local.base}"
    app_insights       = "appi-${local.base}"
    container_env      = "cae-${local.base}"
    container_app      = "ca-${local.base}"
    managed_identity   = "id-${local.base}"
    key_vault          = "kv-${var.workload}-${var.environment}-${local.suffix}"
    container_registry = "cr${local.compact}${local.suffix}"
    postgres           = "pgsql-${var.workload}-${var.environment}-${local.region}-${local.suffix}"
    storage_account    = "st${local.compact}${local.suffix}"
  }

  tags = {
    workload    = var.workload
    environment = var.environment
    managed-by  = "terraform"
    project     = "fca-handbook-assistant"
  }
}
