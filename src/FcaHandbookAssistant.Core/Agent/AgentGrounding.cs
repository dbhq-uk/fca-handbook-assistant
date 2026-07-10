using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Agent;

/// <summary>
/// The agent's grounding guardrail: a cited reference is kept only if it resolves to a real
/// provision in the store (and its URL is replaced with the authoritative one). If nothing
/// resolves, the answer is refused. This is the agent-path analogue of <c>GroundingPolicy</c>,
/// which validates against a per-question retrieval set.
/// </summary>
public static class AgentGrounding
{
    public static async Task<GroundedAnswer> ResolveAgainstStoreAsync(
        IProvisionStore store,
        GroundedAnswer answer,
        CancellationToken cancellationToken = default)
    {
        var kept = new List<Citation>();
        foreach (var citation in answer.Citations)
        {
            var provision = await store.GetByReferenceAsync(citation.Reference, cancellationToken);
            if (provision is not null)
            {
                kept.Add(citation with { Url = provision.Url });
            }
        }

        return kept.Count == 0
            ? GroundedAnswer.Refusal("The agent could not ground its answer in a known provision.")
            : answer with { Citations = kept, Refused = false, Reason = null };
    }
}
