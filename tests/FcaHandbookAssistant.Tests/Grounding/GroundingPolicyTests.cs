using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.Core.Grounding;

namespace FcaHandbookAssistant.Tests.Grounding;

public class GroundingPolicyTests
{
    const double Floor = 0.5;

    static RetrievedProvision Retrieved(string reference, double score) =>
        new(new Provision(reference, "PRIN", "title", "https://handbook.fca.org.uk/x", "body", "hash"), score);

    static GroundedAnswer ModelAnswer(params string[] citedReferences) =>
        new("some answer", citedReferences.Select(r => new Citation(r, "u", "q")).ToArray(), Refused: false, Reason: null);

    [Fact]
    public void Refuses_when_nothing_retrieved()
    {
        var result = GroundingPolicy.Apply(Array.Empty<RetrievedProvision>(), ModelAnswer("PRIN 2.1.1"), Floor);

        Assert.True(result.Refused);
        Assert.Empty(result.Citations);
    }

    [Fact]
    public void Refuses_when_top_score_below_floor()
    {
        var result = GroundingPolicy.Apply(new[] { Retrieved("PRIN 2.1.1", 0.10) }, ModelAnswer("PRIN 2.1.1"), Floor);

        Assert.True(result.Refused);
    }

    [Fact]
    public void Refuses_when_only_citation_is_not_in_retrieved_set()
    {
        // The model cited a provision that was never retrieved: a hallucinated rule.
        var result = GroundingPolicy.Apply(new[] { Retrieved("PRIN 2.1.1", 0.9) }, ModelAnswer("SYSC 9.9.9"), Floor);

        Assert.True(result.Refused);
    }

    [Fact]
    public void Keeps_only_grounded_citations_and_drops_hallucinated_ones()
    {
        var result = GroundingPolicy.Apply(
            new[] { Retrieved("PRIN 2.1.1", 0.9) },
            ModelAnswer("PRIN 2.1.1", "SYSC 9.9.9"),
            Floor);

        Assert.False(result.Refused);
        Assert.Single(result.Citations);
        Assert.Equal("PRIN 2.1.1", result.Citations[0].Reference);
    }

    [Fact]
    public void Matches_references_ignoring_case_and_extra_whitespace()
    {
        var result = GroundingPolicy.Apply(
            new[] { Retrieved("PRIN 2.1.1", 0.9) },
            ModelAnswer("prin   2.1.1"),
            Floor);

        Assert.False(result.Refused);
        Assert.Single(result.Citations);
    }
}
