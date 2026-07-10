using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Ingestion;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Tests.Ingestion;

public class IngestionPipelineTests
{
    [Fact]
    public async Task Ingests_each_entry_with_embedding_and_content_hash()
    {
        var store = new InMemoryProvisionStore();
        var pipeline = new IngestionPipeline(new LocalEmbeddingGenerator(), store);
        var entries = new[]
        {
            new ManifestEntry("PRIN 2.1", "PRIN", "The Principles", "https://handbook.fca.org.uk/handbook/PRIN/2/1.html"),
            new ManifestEntry("SYSC 4.1", "SYSC", "General organisational requirements", "https://handbook.fca.org.uk/handbook/SYSC/4/1.html"),
        };

        var count = await pipeline.RunAsync(entries, (entry, _) => Task.FromResult($"Text for {entry.Reference} - {entry.Title}"));

        Assert.Equal(2, count);
        var prin = await store.GetByReferenceAsync("PRIN 2.1");
        Assert.NotNull(prin);
        Assert.Equal("PRIN", prin!.Sourcebook);
        Assert.False(string.IsNullOrEmpty(prin.ContentHash));
        Assert.NotNull(await store.GetByReferenceAsync("SYSC 4.1"));
    }
}
