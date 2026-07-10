using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.Core.Retrieval;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Tests.Retrieval;

[Collection("database")]
public class PgVectorRetrieverTests
{
    readonly DatabaseFixture _db;
    readonly LocalEmbeddingGenerator _embeddings = new();

    public PgVectorRetrieverTests(DatabaseFixture db) => _db = db;

    async Task ResetAsync()
    {
        await using var command = _db.DataSource!.CreateCommand("TRUNCATE provisions;");
        await command.ExecuteNonQueryAsync();
    }

    async Task<ReadOnlyMemory<float>> EmbedAsync(string text) =>
        (await _embeddings.GenerateAsync([text]))[0].Vector;

    async Task SeedAsync(IProvisionStore store, string reference, string text)
    {
        var sourcebook = reference.Split(' ')[0];
        var provision = new Provision(
            reference, sourcebook, "title", $"https://handbook.fca.org.uk/handbook/{sourcebook}", text, "hash");
        await store.UpsertAsync(provision, await EmbedAsync(text));
    }

    [RequiresDatabaseFact]
    public async Task Retrieves_most_similar_provision_first()
    {
        await ResetAsync();
        var store = new PgVectorProvisionStore(_db.DataSource!);
        await SeedAsync(store, "PRIN 1.1.1", "A firm must conduct its business with integrity");
        await SeedAsync(store, "COBS 9.2.1", "assessing the suitability of a client investment");
        await SeedAsync(store, "SYSC 4.1.1", "robust governance arrangements and internal controls");

        var retriever = new PgVectorRetriever(_db.DataSource!);
        var results = await retriever.RetrieveAsync(await EmbedAsync("conduct business with integrity"), k: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("PRIN 1.1.1", results[0].Provision.Reference);
        Assert.True(results[0].Score >= results[1].Score);
    }

    [RequiresDatabaseFact]
    public async Task GetByReference_round_trips_and_upsert_is_idempotent()
    {
        await ResetAsync();
        var store = new PgVectorProvisionStore(_db.DataSource!);
        await SeedAsync(store, "PRIN 2.1.1", "integrity");
        await SeedAsync(store, "PRIN 2.1.1", "integrity updated"); // upsert on the same reference

        var fetched = await store.GetByReferenceAsync("PRIN 2.1.1");
        Assert.NotNull(fetched);
        Assert.Equal("integrity updated", fetched!.ChunkText);

        await using var command = _db.DataSource!.CreateCommand(
            "SELECT count(*) FROM provisions WHERE reference = 'PRIN 2.1.1';");
        var count = (long)(await command.ExecuteScalarAsync())!;
        Assert.Equal(1L, count);
    }

    [RequiresDatabaseFact]
    public async Task GetByReference_returns_null_when_absent()
    {
        await ResetAsync();
        var store = new PgVectorProvisionStore(_db.DataSource!);

        Assert.Null(await store.GetByReferenceAsync("NOT 9.9.9"));
    }
}
