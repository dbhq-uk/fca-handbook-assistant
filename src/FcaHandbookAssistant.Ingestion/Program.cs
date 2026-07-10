// Ingests the curated manifest of FCA Handbook provisions into pgvector.
//
// Env:
//   FCA_DB                              Postgres connection string (required)
//   AZURE_OPENAI_ENDPOINT               Foundry/Azure OpenAI endpoint (live embeddings)
//   AZURE_OPENAI_EMBEDDING_DEPLOYMENT   embedding deployment name (default text-embedding-3-small)
//   AZURE_OPENAI_KEY                    optional; falls back to DefaultAzureCredential
//
// Flags:
//   --dry-run   use local deterministic embeddings (no cloud, no cost)
using System.Text.Json;
using Azure.Identity;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Azure;
using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.Core.Ingestion;
using FcaHandbookAssistant.Core.Retrieval;
using Microsoft.Extensions.AI;

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

await using var dataSource = DataSourceFactory.Create(connectionString);
await SchemaBootstrapper.EnsureAsync(dataSource);
var store = new PgVectorProvisionStore(dataSource);

IEmbeddingGenerator<string, Embedding<float>> embeddings;
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
if (!dryRun && !string.IsNullOrWhiteSpace(endpoint))
{
    var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? "text-embedding-3-small";
    var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
    var client = string.IsNullOrWhiteSpace(key)
        ? AzureOpenAIClientFactory.Create(new Uri(endpoint), new DefaultAzureCredential())
        : AzureOpenAIClientFactory.Create(new Uri(endpoint), key);
    embeddings = AzureOpenAIClientFactory.EmbeddingGenerator(client, deployment);
    Console.WriteLine($"Embeddings: Azure OpenAI ({deployment}).");
}
else
{
    embeddings = new LocalEmbeddingGenerator();
    Console.WriteLine("Embeddings: local deterministic (no cloud).");
}

var pipeline = new IngestionPipeline(embeddings, store);
var count = await pipeline.RunAsync(entries, (entry, _) => Task.FromResult(TextFor(entry)));

Console.WriteLine($"Ingested {count} provisions.");
