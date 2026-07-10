// Ingests the curated manifest of FCA Handbook provisions into pgvector.
//
// Env:
//   FCA_DB                              Postgres connection string (required)
//   EMBEDDINGS                          local (default) | ollama | azure
//   EMBEDDING_DIM                       vector dimension (default 1536; 384 for ollama all-minilm)
//   OLLAMA_ENDPOINT / OLLAMA_MODEL      Ollama OpenAI-compatible endpoint + model
//   AZURE_OPENAI_ENDPOINT / _EMBEDDING_DEPLOYMENT / _KEY   Azure OpenAI (key optional; else managed identity)
//   FCA_TEXT_FILE                       JSON of real Handbook text (see scripts/fetch_handbook.py)
//
// Flags:
//   --dry-run   force local deterministic embeddings (no external service)
using System.Text.Json;
using FcaHandbookAssistant.Core.Ai;
using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.Core.Ingestion;
using FcaHandbookAssistant.Core.Retrieval;

var dryRun = args.Contains("--dry-run");

var connectionString = Environment.GetEnvironmentVariable("FCA_DB")
    ?? throw new InvalidOperationException("Set FCA_DB to the Postgres connection string.");

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

var manifestPath = Path.Combine(AppContext.BaseDirectory, "data", "manifest.json");
var manifestJson = await File.ReadAllTextAsync(manifestPath);
var entries = JsonSerializer.Deserialize<ManifestEntry[]>(manifestJson, jsonOptions)
    ?? throw new InvalidOperationException("The manifest could not be read.");

// Real Handbook text, if a fetch has populated it (see scripts/fetch_handbook.py). When present it
// is used; otherwise we fall back to the manifest summary (offline/CI). The file is gitignored - the
// Handbook is Crown/FCA copyright, stored for retrieval, never committed.
var textFile = Environment.GetEnvironmentVariable("FCA_TEXT_FILE");
var realText = !string.IsNullOrWhiteSpace(textFile) && File.Exists(textFile)
    ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(textFile), jsonOptions) ?? []
    : [];
Console.WriteLine(realText.Count > 0 ? $"Real Handbook text: {realText.Count} provisions from {textFile}." : "Real Handbook text: none (using manifest summaries).");

string TextFor(ManifestEntry entry) =>
    realText.TryGetValue(entry.Reference, out var text) && !string.IsNullOrWhiteSpace(text) ? text : entry.Summary;

var provider = dryRun
    ? EmbeddingProvider.Local
    : (Environment.GetEnvironmentVariable("EMBEDDINGS") ?? "local").ToLowerInvariant() switch
    {
        "ollama" => EmbeddingProvider.Ollama,
        "azure" => EmbeddingProvider.Azure,
        _ => EmbeddingProvider.Local,
    };
var defaultDimensions = provider == EmbeddingProvider.Ollama ? 384 : 1536;
var dimensions = int.TryParse(Environment.GetEnvironmentVariable("EMBEDDING_DIM"), out var configured) ? configured : defaultDimensions;

var settings = new EmbeddingSettings
{
    Provider = provider,
    Dimensions = dimensions,
    OllamaEndpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434/v1",
    OllamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "all-minilm",
    AzureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
    AzureDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? "text-embedding-3-small",
    AzureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY"),
};
var embeddings = EmbeddingGeneratorFactory.Create(settings);
Console.WriteLine($"Embeddings: {provider} (dim {dimensions}).");

await using var dataSource = DataSourceFactory.Create(connectionString);
await SchemaBootstrapper.EnsureAsync(dataSource, dimensions);
var store = new PgVectorProvisionStore(dataSource);

var pipeline = new IngestionPipeline(embeddings, store);
var count = await pipeline.RunAsync(entries, (entry, _) => Task.FromResult(TextFor(entry)));

Console.WriteLine($"Ingested {count} provisions.");
