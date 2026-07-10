using FcaHandbookAssistant.Core.Agent;
using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Tests.Agent;

public class AgentGroundingTests
{
    static async Task<InMemoryProvisionStore> StoreWith(params string[] references)
    {
        var store = new InMemoryProvisionStore();
        foreach (var reference in references)
        {
            await store.UpsertAsync(
                new Provision(reference, reference.Split(' ')[0], "title", $"https://handbook.fca.org.uk/{reference}", "body", "h"),
                default);
        }

        return store;
    }

    static GroundedAnswer AnswerCiting(params string[] references) =>
        new("text", references.Select(r => new Citation(r, "https://example.com/wrong", "quote")).ToArray(), Refused: false, Reason: null);

    [Fact]
    public async Task Keeps_citations_that_resolve_and_uses_the_authoritative_url()
    {
        var store = await StoreWith("PRIN 2.1.1");

        var result = await AgentGrounding.ResolveAgainstStoreAsync(store, AnswerCiting("PRIN 2.1.1"));

        Assert.False(result.Refused);
        Assert.Single(result.Citations);
        Assert.Equal("https://handbook.fca.org.uk/PRIN 2.1.1", result.Citations[0].Url);
    }

    [Fact]
    public async Task Drops_citations_that_do_not_resolve()
    {
        var store = await StoreWith("PRIN 2.1.1");

        var result = await AgentGrounding.ResolveAgainstStoreAsync(store, AnswerCiting("PRIN 2.1.1", "SYSC 9.9.9"));

        Assert.Single(result.Citations);
        Assert.Equal("PRIN 2.1.1", result.Citations[0].Reference);
    }

    [Fact]
    public async Task Refuses_when_no_citation_resolves()
    {
        var store = await StoreWith("PRIN 2.1.1");

        var result = await AgentGrounding.ResolveAgainstStoreAsync(store, AnswerCiting("SYSC 9.9.9"));

        Assert.True(result.Refused);
    }
}
