using System.Text;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Core.Ai.Local;

/// <summary>
/// A deterministic, offline embedding generator using the signed hashing trick (bag of words).
/// The same text always yields the same unit vector across processes and runs, and texts that
/// share words have positive cosine similarity - enough to drive retrieval locally and in CI
/// without a cloud model. It is not semantic: real semantic retrieval uses the Azure embedding
/// deployment behind the same <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> interface.
/// </summary>
public sealed class LocalEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    readonly int _dimensions;

    public LocalEmbeddingGenerator(int dimensions = 1536) => _dimensions = dimensions;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var value in values)
        {
            results.Add(new Embedding<float>(Embed(value)));
        }

        return Task.FromResult(results);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
        // Nothing to dispose.
    }

    ReadOnlyMemory<float> Embed(string text)
    {
        var vector = new float[_dimensions];
        foreach (var token in Tokenise(text))
        {
            var hash = Fnv1a(token);
            var bucket = (int)(hash % (uint)_dimensions);
            var sign = (hash & 1) == 0 ? 1f : -1f;
            vector[bucket] += sign;
        }

        Normalise(vector);
        return vector;
    }

    static IEnumerable<string> Tokenise(string text)
    {
        var token = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                token.Append(char.ToLowerInvariant(ch));
            }
            else if (token.Length > 0)
            {
                yield return token.ToString();
                token.Clear();
            }
        }

        if (token.Length > 0)
        {
            yield return token.ToString();
        }
    }

    // FNV-1a: a stable hash. (string.GetHashCode is randomised per process and must not be used.)
    static uint Fnv1a(string token)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var ch in token)
        {
            hash ^= ch;
            hash *= prime;
        }

        return hash;
    }

    static void Normalise(float[] vector)
    {
        double sumOfSquares = 0;
        foreach (var value in vector)
        {
            sumOfSquares += value * (double)value;
        }

        if (sumOfSquares <= 0)
        {
            return;
        }

        var inverseNorm = (float)(1.0 / Math.Sqrt(sumOfSquares));
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] *= inverseNorm;
        }
    }
}
