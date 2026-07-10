using Azure;
using Azure.AI.ContentSafety;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using FcaHandbookAssistant.Api.Configuration;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Agent;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Azure;
using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.Core.Grounding;
using FcaHandbookAssistant.Core.Guardrails;
using FcaHandbookAssistant.Core.Retrieval;
using Microsoft.Extensions.AI;
using Npgsql;

namespace FcaHandbookAssistant.Api;

/// <summary>Wires the grounded answer pipeline and agent, selecting Local or Azure implementations.</summary>
public static class HandbookAssistantServiceCollectionExtensions
{
    public static IServiceCollection AddHandbookAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? options.PostgresConnectionString
            ?? Environment.GetEnvironmentVariable("FCA_DB")
            ?? throw new InvalidOperationException("No Postgres connection string configured (ConnectionStrings:Postgres or FCA_DB).");

        services.AddSingleton(_ => DataSourceFactory.Create(connectionString));
        services.AddSingleton<IProvisionStore>(sp => new PgVectorProvisionStore(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IRetriever>(sp => new PgVectorRetriever(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IAuditSink>(sp => new PostgresAuditSink(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IPiiRedactor, RegexPiiRedactor>();
        services.AddSingleton(new GroundedAnswerOptions
        {
            RetrievalCount = options.RetrievalCount,
            RetrievalFloor = options.RetrievalFloor,
            Model = options.IsAzure ? options.Azure.ChatDeployment : "local-grounded",
        });

        if (options.IsAzure)
        {
            AddAzure(services, options.Azure, options);
        }
        else
        {
            services.AddSingleton<IChatClient, LocalGroundedChatClient>();
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new LocalEmbeddingGenerator());
            services.AddSingleton<IContentSafetyClient, PassThroughContentSafety>();
            services.AddSingleton<IHandbookAgent, GroundedAnswerAgentAdapter>();
        }

        services.AddSingleton<IGroundedAnswerService, GroundedAnswerService>();
        return services;
    }

    static void AddAzure(IServiceCollection services, AzureAiOptions azure, AiOptions options)
    {
        var endpoint = new Uri(azure.OpenAiEndpoint ?? throw new InvalidOperationException("Ai:Azure:OpenAiEndpoint is required in Azure mode."));
        var openAiClient = string.IsNullOrWhiteSpace(azure.OpenAiKey)
            ? AzureOpenAIClientFactory.Create(endpoint, new DefaultAzureCredential())
            : AzureOpenAIClientFactory.Create(endpoint, azure.OpenAiKey);

        services.AddSingleton<IChatClient>(_ => AzureOpenAIClientFactory.ChatClient(openAiClient, azure.ChatDeployment));
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ =>
            AzureOpenAIClientFactory.EmbeddingGenerator(openAiClient, azure.EmbeddingDeployment));

        if (!string.IsNullOrWhiteSpace(azure.ContentSafetyEndpoint))
        {
            var csEndpoint = new Uri(azure.ContentSafetyEndpoint);
            var contentSafety = string.IsNullOrWhiteSpace(azure.ContentSafetyKey)
                ? new ContentSafetyClient(csEndpoint, new DefaultAzureCredential())
                : new ContentSafetyClient(csEndpoint, new AzureKeyCredential(azure.ContentSafetyKey));
            services.AddSingleton<IContentSafetyClient>(_ => new AzureContentSafetyClient(contentSafety));
        }
        else
        {
            services.AddSingleton<IContentSafetyClient, PassThroughContentSafety>();
        }

        var projectEndpoint = new Uri(azure.FoundryProjectEndpoint ?? azure.OpenAiEndpoint!);
        services.AddSingleton<IHandbookAgent>(sp => new FoundryHandbookAgent(
            new AIProjectClient(projectEndpoint, new DefaultAzureCredential()),
            azure.ChatDeployment,
            new HandbookTools(
                sp.GetRequiredService<IProvisionStore>(),
                sp.GetRequiredService<IRetriever>(),
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                options.RetrievalCount,
                options.RetrievalFloor),
            sp.GetRequiredService<IProvisionStore>(),
            sp.GetRequiredService<IAuditSink>()));
    }
}
