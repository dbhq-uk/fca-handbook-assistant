using FcaHandbookAssistant.Core.Ai.Local;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Tests.Ai;

public class LocalEmbeddingGeneratorTests
{
    static async Task<float[]> Embed(LocalEmbeddingGenerator generator, string text)
    {
        var embeddings = await generator.GenerateAsync([text]);
        return embeddings[0].Vector.ToArray();
    }

    // Vectors are unit-normalised, so the dot product is the cosine similarity.
    static double Cosine(float[] a, float[] b)
    {
        double dot = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
        }

        return dot;
    }

    [Fact]
    public async Task Same_text_yields_identical_vector()
    {
        var generator = new LocalEmbeddingGenerator();

        var a = await Embed(generator, "capital adequacy requirements");
        var b = await Embed(generator, "capital adequacy requirements");

        Assert.Equal(a, b);
    }

    [Fact]
    public async Task Default_dimension_is_1536()
    {
        var generator = new LocalEmbeddingGenerator();

        var vector = await Embed(generator, "anything");

        Assert.Equal(1536, vector.Length);
    }

    [Fact]
    public async Task Shared_words_are_more_similar_than_unrelated_text()
    {
        var generator = new LocalEmbeddingGenerator();

        var query = await Embed(generator, "client money segregation rules");
        var related = await Embed(generator, "rules on segregation of client money");
        var unrelated = await Embed(generator, "capital buffers for market risk");

        Assert.True(Cosine(query, related) > Cosine(query, unrelated));
    }
}
