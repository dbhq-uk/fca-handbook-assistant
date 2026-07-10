using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace FcaHandbookAssistant.Core.Retrieval;

/// <summary>
/// The Azure AI Search index for provisions - the cloud parity variant of the pgvector store.
/// Field names are shared by the store and retriever; <see cref="EnsureAsync"/> creates or
/// updates the index with an HNSW vector profile over the 1536-dimension embedding.
/// </summary>
public static class AzureSearchIndex
{
    public const string Reference = "reference";
    public const string Sourcebook = "sourcebook";
    public const string Title = "title";
    public const string Url = "url";
    public const string ChunkText = "chunkText";
    public const string ContentHash = "contentHash";
    public const string Embedding = "embedding";

    const string Profile = "hnsw-profile";
    const string Algorithm = "hnsw-config";
    const int Dimensions = 1536;

    public static async Task EnsureAsync(SearchIndexClient indexClient, string indexName, CancellationToken cancellationToken = default)
    {
        var index = new SearchIndex(indexName)
        {
            Fields =
            {
                new SearchField(Reference, SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchField(Sourcebook, SearchFieldDataType.String) { IsFilterable = true },
                new SearchField(Title, SearchFieldDataType.String),
                new SearchField(Url, SearchFieldDataType.String),
                new SearchField(ChunkText, SearchFieldDataType.String) { IsSearchable = true },
                new SearchField(ContentHash, SearchFieldDataType.String),
                new SearchField(Embedding, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = Dimensions,
                    VectorSearchProfileName = Profile,
                },
            },
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile(Profile, Algorithm) },
                Algorithms = { new HnswAlgorithmConfiguration(Algorithm) },
            },
        };

        await indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
    }
}
