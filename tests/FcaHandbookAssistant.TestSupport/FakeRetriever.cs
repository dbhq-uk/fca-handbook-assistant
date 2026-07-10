using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.TestSupport;

/// <summary>An <see cref="IRetriever"/> that returns a fixed, pre-scored result set (top-k).</summary>
public sealed class FakeRetriever : IRetriever
{
    readonly IReadOnlyList<RetrievedProvision> _results;

    public FakeRetriever(params RetrievedProvision[] results) => _results = results;

    public Task<IReadOnlyList<RetrievedProvision>> RetrieveAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int k,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RetrievedProvision>>(_results.Take(k).ToList());
}
