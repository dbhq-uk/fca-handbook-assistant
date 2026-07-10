using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Abstractions;

/// <summary>Retrieves the provisions most similar to a query embedding, best match first.</summary>
public interface IRetriever
{
    Task<IReadOnlyList<RetrievedProvision>> RetrieveAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int k,
        CancellationToken cancellationToken = default);
}
