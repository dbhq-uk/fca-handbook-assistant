using System.ClientModel;
using Azure.Identity;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Azure;
using Microsoft.Extensions.AI;
using OpenAI;

namespace FcaHandbookAssistant.Core.Ai;

public enum EmbeddingProvider
{
    /// <summary>Deterministic hashing-trick embeddings - zero dependencies, for offline determinism.</summary>
    Local,

    /// <summary>Real semantic embeddings from a local Ollama server (OpenAI-compatible endpoint).</summary>
    Ollama,

    /// <summary>Azure OpenAI / Foundry embeddings.</summary>
    Azure,
}

/// <summary>Settings for building an embedding generator. <see cref="Dimensions"/> must match the model.</summary>
public sealed class EmbeddingSettings
{
    public EmbeddingProvider Provider { get; init; } = EmbeddingProvider.Local;
    public int Dimensions { get; init; } = 1536;

    public string OllamaEndpoint { get; init; } = "http://localhost:11434/v1";
    public string OllamaModel { get; init; } = "all-minilm";

    public string? AzureEndpoint { get; init; }
    public string AzureDeployment { get; init; } = "text-embedding-3-small";
    public string? AzureKey { get; init; }
}

/// <summary>
/// Builds an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for the configured provider.
/// Ollama and Azure OpenAI are reached through the same OpenAI client abstraction; the local
/// provider is a deterministic offline fallback. The store's vector dimension must match
/// <see cref="EmbeddingSettings.Dimensions"/>.
/// </summary>
public static class EmbeddingGeneratorFactory
{
    public static IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingSettings settings) => settings.Provider switch
    {
        EmbeddingProvider.Local => new LocalEmbeddingGenerator(settings.Dimensions),
        EmbeddingProvider.Ollama => Ollama(settings),
        EmbeddingProvider.Azure => Azure(settings),
        _ => throw new ArgumentOutOfRangeException(nameof(settings)),
    };

    static IEmbeddingGenerator<string, Embedding<float>> Ollama(EmbeddingSettings settings)
    {
        // Ollama exposes an OpenAI-compatible API; the key is ignored but must be non-empty.
        var client = new OpenAIClient(new ApiKeyCredential("ollama"), new OpenAIClientOptions { Endpoint = new Uri(settings.OllamaEndpoint) });
        return client.GetEmbeddingClient(settings.OllamaModel).AsIEmbeddingGenerator();
    }

    static IEmbeddingGenerator<string, Embedding<float>> Azure(EmbeddingSettings settings)
    {
        var endpoint = new Uri(settings.AzureEndpoint ?? throw new InvalidOperationException("Azure embedding endpoint is required."));
        var client = string.IsNullOrWhiteSpace(settings.AzureKey)
            ? AzureOpenAIClientFactory.Create(endpoint, new DefaultAzureCredential())
            : AzureOpenAIClientFactory.Create(endpoint, settings.AzureKey);
        return AzureOpenAIClientFactory.EmbeddingGenerator(client, settings.AzureDeployment, settings.Dimensions);
    }
}
