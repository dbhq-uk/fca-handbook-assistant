using System.Text.Json;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Agent;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Tests.Agent;

public class HandbookToolsTests
{
    static readonly LocalEmbeddingGenerator Embeddings = new();

    static RetrievedProvision Retrieved(string reference, double score) =>
        new(new Provision(reference, reference.Split(' ')[0], "title", $"https://handbook.fca.org.uk/{reference}", "body", "hash"), score);

    static HandbookTools Build(IProvisionStore store, params RetrievedProvision[] retrieved) =>
        new(store, new FakeRetriever(retrieved), Embeddings, floor: 0.3);

    static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task LookupRule_returns_the_provision_when_present()
    {
        var store = new InMemoryProvisionStore();
        await store.UpsertAsync(new Provision("PRIN 2.1.1", "PRIN", "Integrity", "https://handbook.fca.org.uk/PRIN/2/1", "A firm must act with integrity.", "h"), default);
        var tools = Build(store);

        var result = Parse(await tools.LookupRuleAsync("PRIN 2.1.1"));

        Assert.True(result.GetProperty("found").GetBoolean());
        Assert.Equal("PRIN 2.1.1", result.GetProperty("reference").GetString());
        Assert.Contains("integrity", result.GetProperty("text").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupRule_reports_not_found_for_an_unknown_reference()
    {
        var tools = Build(new InMemoryProvisionStore());

        var result = Parse(await tools.LookupRuleAsync("SYSC 9.9.9"));

        Assert.False(result.GetProperty("found").GetBoolean());
    }

    [Fact]
    public async Task FindRelatedRules_returns_only_matches_above_the_floor()
    {
        var tools = Build(new InMemoryProvisionStore(), Retrieved("PRIN 2.1.1", 0.9), Retrieved("COBS 9.2.1", 0.10));

        var related = Parse(await tools.FindRelatedRulesAsync("integrity")).GetProperty("related");

        Assert.Equal(1, related.GetArrayLength());
        Assert.Equal("PRIN 2.1.1", related[0].GetProperty("reference").GetString());
    }

    [Fact]
    public async Task CheckScenario_reports_applicable_sections_for_a_strong_match()
    {
        var tools = Build(new InMemoryProvisionStore(), Retrieved("SYSC 4.1.1", 0.8));

        var result = Parse(await tools.CheckScenarioAsync("we have weak internal controls"));

        Assert.True(result.GetProperty("applicable").GetBoolean());
        Assert.Equal("SYSC 4.1.1", result.GetProperty("sections")[0].GetProperty("reference").GetString());
    }

    [Fact]
    public async Task CheckScenario_refuses_on_a_weak_match()
    {
        var tools = Build(new InMemoryProvisionStore(), Retrieved("PRIN 2.1.1", 0.10));

        var result = Parse(await tools.CheckScenarioAsync("something entirely unrelated"));

        Assert.False(result.GetProperty("applicable").GetBoolean());
    }
}
