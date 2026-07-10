namespace FcaHandbookAssistant.Api.Configuration;

/// <summary>Binds the <c>Ai</c> configuration section. <see cref="Mode"/> selects Local or Azure wiring.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Mode { get; set; } = "Local";
    public string? PostgresConnectionString { get; set; }
    public int RetrievalCount { get; set; } = 5;
    public double RetrievalFloor { get; set; } = 0.3;
    public AzureAiOptions Azure { get; set; } = new();
    public EmbeddingOptions Embeddings { get; set; } = new();

    public bool IsAzure => string.Equals(Mode, "Azure", StringComparison.OrdinalIgnoreCase);
}

public sealed class EmbeddingOptions
{
    /// <summary>Local | Ollama | Azure. When unset, derived from <see cref="AiOptions.Mode"/>.</summary>
    public string? Provider { get; set; }
    public int Dimensions { get; set; } = 1536;
    public string OllamaEndpoint { get; set; } = "http://localhost:11434/v1";
    public string OllamaModel { get; set; } = "all-minilm";
}

public sealed class AzureAiOptions
{
    public string? OpenAiEndpoint { get; set; }
    public string ChatDeployment { get; set; } = "gpt-4o-mini";
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";
    public string? OpenAiKey { get; set; }
    public string? ContentSafetyEndpoint { get; set; }
    public string? ContentSafetyKey { get; set; }
    public string? FoundryProjectEndpoint { get; set; }
}
