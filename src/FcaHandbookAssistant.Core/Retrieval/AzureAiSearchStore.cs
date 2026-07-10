using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Retrieval;

/// <summary>An <see cref="IProvisionStore"/> backed by an Azure AI Search index.</summary>
public sealed class AzureAiSearchStore : IProvisionStore
{
    readonly SearchClient _searchClient;

    public AzureAiSearchStore(SearchClient searchClient) => _searchClient = searchClient;

    public async Task UpsertAsync(Provision provision, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default)
    {
        var document = new SearchDocument
        {
            [AzureSearchIndex.Reference] = provision.Reference,
            [AzureSearchIndex.Sourcebook] = provision.Sourcebook,
            [AzureSearchIndex.Title] = provision.Title,
            [AzureSearchIndex.Url] = provision.Url,
            [AzureSearchIndex.ChunkText] = provision.ChunkText,
            [AzureSearchIndex.ContentHash] = provision.ContentHash,
            [AzureSearchIndex.Embedding] = embedding.ToArray(),
        };

        await _searchClient.MergeOrUploadDocumentsAsync([document], cancellationToken: cancellationToken);
    }

    public async Task<Provision?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _searchClient.GetDocumentAsync<SearchDocument>(reference, cancellationToken: cancellationToken);
            return ToProvision(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    internal static Provision ToProvision(SearchDocument document) => new(
        document.GetString(AzureSearchIndex.Reference),
        document.GetString(AzureSearchIndex.Sourcebook),
        document.GetString(AzureSearchIndex.Title),
        document.GetString(AzureSearchIndex.Url),
        document.GetString(AzureSearchIndex.ChunkText),
        document.GetString(AzureSearchIndex.ContentHash));
}
