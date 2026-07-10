using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Core.Azure;

/// <summary>
/// Builds the Azure OpenAI-backed <see cref="IChatClient"/> and embedding generator used by the
/// grounded pipeline and ingestion. The endpoint is an Azure AI Foundry project's OpenAI endpoint;
/// authentication prefers a managed identity (<see cref="TokenCredential"/>) with an API key fallback.
/// </summary>
public static class AzureOpenAIClientFactory
{
    public static AzureOpenAIClient Create(Uri endpoint, TokenCredential credential) => new(endpoint, credential);

    public static AzureOpenAIClient Create(Uri endpoint, string apiKey) => new(endpoint, new ApiKeyCredential(apiKey));

    public static IChatClient ChatClient(AzureOpenAIClient client, string deployment) =>
        client.GetChatClient(deployment).AsIChatClient();

    public static IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator(
        AzureOpenAIClient client, string deployment, int dimensions = 1536) =>
        client.GetEmbeddingClient(deployment).AsIEmbeddingGenerator(dimensions);
}
