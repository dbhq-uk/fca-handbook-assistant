using System.Text.Json;
using FcaHandbookAssistant.Core.Abstractions;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Core.Agent;

/// <summary>
/// The functions the tool-calling agent can invoke, backed by the same store and retriever as
/// the RAG pipeline. Each returns a small JSON payload for the model to reason over. They never
/// invent provisions: a lookup miss or a weak match is reported honestly.
/// </summary>
public sealed class HandbookTools
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    readonly IProvisionStore _store;
    readonly IRetriever _retriever;
    readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    readonly int _count;
    readonly double _floor;

    public HandbookTools(
        IProvisionStore store,
        IRetriever retriever,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        int count = 5,
        double floor = 0.3)
    {
        _store = store;
        _retriever = retriever;
        _embeddings = embeddings;
        _count = count;
        _floor = floor;
    }

    /// <summary>Fetch a single provision by its exact reference (for example <c>PRIN 2.1.1</c>).</summary>
    public async Task<string> LookupRuleAsync(string reference, CancellationToken cancellationToken = default)
    {
        var provision = await _store.GetByReferenceAsync(reference, cancellationToken);
        return provision is null
            ? Serialize(new { found = false, reference })
            : Serialize(new { found = true, provision.Reference, provision.Title, provision.Url, text = provision.ChunkText });
    }

    /// <summary>Find provisions related to a topic, for cross-referencing.</summary>
    public async Task<string> FindRelatedRulesAsync(string topic, CancellationToken cancellationToken = default)
    {
        var related = await SearchAsync(topic, cancellationToken);
        return Serialize(new
        {
            topic,
            related = related.Select(r => new { r.Provision.Reference, r.Provision.Url, score = r.Score }),
        });
    }

    /// <summary>Map a described scenario to the applicable provisions; refuse on a weak match.</summary>
    public async Task<string> CheckScenarioAsync(string description, CancellationToken cancellationToken = default)
    {
        var applicable = await SearchAsync(description, cancellationToken);
        return applicable.Count == 0
            ? Serialize(new { applicable = false, reason = "No sufficiently relevant provisions were found." })
            : Serialize(new
            {
                applicable = true,
                sections = applicable.Select(r => new { r.Provision.Reference, r.Provision.Url, score = r.Score }),
            });
    }

    async Task<IReadOnlyList<Domain.RetrievedProvision>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        var embedding = (await _embeddings.GenerateAsync([text], cancellationToken: cancellationToken))[0].Vector;
        var results = await _retriever.RetrieveAsync(embedding, _count, cancellationToken);
        return results.Where(r => r.Score >= _floor).ToArray();
    }

    static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);
}
