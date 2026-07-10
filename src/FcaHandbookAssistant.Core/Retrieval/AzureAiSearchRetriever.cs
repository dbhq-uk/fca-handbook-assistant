using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Retrieval;

/// <summary>
/// Retrieves provisions from Azure AI Search using a vector (k-NN) query over the embedding
/// field - the cloud parity variant of <see cref="PgVectorRetriever"/>, behind the same
/// <see cref="IRetriever"/> interface.
/// </summary>
public sealed class AzureAiSearchRetriever : IRetriever
{
    readonly SearchClient _searchClient;

    public AzureAiSearchRetriever(SearchClient searchClient) => _searchClient = searchClient;

    public async Task<IReadOnlyList<RetrievedProvision>> RetrieveAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int k,
        CancellationToken cancellationToken = default)
    {
        var vectorQuery = new VectorizedQuery(queryEmbedding) { KNearestNeighborsCount = k };
        vectorQuery.Fields.Add(AzureSearchIndex.Embedding);

        var options = new SearchOptions
        {
            Size = k,
            VectorSearch = new VectorSearchOptions { Queries = { vectorQuery } },
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(searchText: null, options, cancellationToken);

        var results = new List<RetrievedProvision>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(new RetrievedProvision(AzureAiSearchStore.ToProvision(result.Document), result.Score ?? 0));
        }

        return results;
    }
}
