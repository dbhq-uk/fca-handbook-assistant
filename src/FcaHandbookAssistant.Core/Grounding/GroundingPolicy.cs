using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Grounding;

/// <summary>
/// The deterministic guardrail that turns a model answer into a grounded answer or a refusal.
/// It never calls a model: given what was retrieved and what the model claimed to cite, it
/// (a) refuses when retrieval is too weak to answer, and (b) drops any citation to a provision
/// that was not actually retrieved - a hallucinated rule - refusing if none survive.
/// </summary>
public static class GroundingPolicy
{
    /// <summary>
    /// Applies the grounding rules.
    /// Refuses when nothing was retrieved or the best score is below <paramref name="retrievalFloor"/>.
    /// Otherwise keeps only citations whose reference matches a retrieved provision (ignoring case
    /// and surrounding whitespace); refuses if that leaves no citations.
    /// </summary>
    public static GroundedAnswer Apply(
        IReadOnlyList<RetrievedProvision> retrieved,
        GroundedAnswer modelAnswer,
        double retrievalFloor)
    {
        if (retrieved.Count == 0 || retrieved.Max(r => r.Score) < retrievalFloor)
        {
            return GroundedAnswer.Refusal("Not found in the provisions available.");
        }

        var retrievedReferences = retrieved
            .Select(r => NormaliseReference(r.Provision.Reference))
            .ToHashSet();

        var groundedCitations = modelAnswer.Citations
            .Where(c => retrievedReferences.Contains(NormaliseReference(c.Reference)))
            .ToArray();

        if (groundedCitations.Length == 0)
        {
            return GroundedAnswer.Refusal("Could not ground an answer in the retrieved provisions.");
        }

        return modelAnswer with { Citations = groundedCitations, Refused = false, Reason = null };
    }

    /// <summary>Normalises a provision reference for comparison: upper-cased, whitespace collapsed.</summary>
    public static string NormaliseReference(string reference) =>
        string.Join(
            ' ',
            reference.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();
}
