using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Core.Ingestion;

/// <summary>
/// Ingests manifest entries into a provision store: for each entry, obtain the text (fetched and
/// chunked, or a placeholder for a dry run), embed it, and upsert. The text source is injected so
/// the pipeline is testable offline and reusable for both the live and dry-run paths.
/// </summary>
public sealed class IngestionPipeline
{
    readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    readonly IProvisionStore _store;

    public IngestionPipeline(IEmbeddingGenerator<string, Embedding<float>> embeddings, IProvisionStore store)
    {
        _embeddings = embeddings;
        _store = store;
    }

    public async Task<int> RunAsync(
        IEnumerable<ManifestEntry> entries,
        Func<ManifestEntry, CancellationToken, Task<string>> textProvider,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var entry in entries)
        {
            var text = await textProvider(entry, cancellationToken);
            var embedding = (await _embeddings.GenerateAsync([text], cancellationToken: cancellationToken))[0].Vector;
            var provision = new Provision(
                entry.Reference, entry.Sourcebook, entry.Title, entry.Url, text, ProvisionChunker.ContentHash(text));
            await _store.UpsertAsync(provision, embedding, cancellationToken);
            count++;
        }

        return count;
    }
}
