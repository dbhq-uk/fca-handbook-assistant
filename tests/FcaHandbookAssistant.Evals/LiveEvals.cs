using Azure.Identity;
using FcaHandbookAssistant.Core.Ai;
using FcaHandbookAssistant.Core.Azure;
using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.Core.Grounding;
using FcaHandbookAssistant.Core.Guardrails;
using FcaHandbookAssistant.Core.Retrieval;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Evals;

/// <summary>
/// The live quality evals: run the gold set against the real Azure model and pgvector, write a
/// markdown report, and assert the regulated thresholds. Env-gated (see <see cref="LiveEvalFactAttribute"/>);
/// run in the deploy session.
/// </summary>
public class LiveEvals
{
    [LiveEvalFact]
    public async Task Gold_set_meets_thresholds_and_writes_a_report()
    {
        var (service, model) = BuildAzureService();
        var results = new List<EvalResult>();

        foreach (var goldCase in GoldSet.Load())
        {
            var answer = await service.AskAsync(goldCase.Question);
            results.Add(new EvalResult(
                goldCase.Question,
                goldCase.ToExpectation(),
                new EvalOutcome(answer.Refused, answer.Citations.Select(c => c.Reference).ToList())));
        }

        var markdown = ReportWriter.BuildMarkdown(results, model, PromptTemplates.PromptVersion, DateTimeOffset.UtcNow);
        var reportPath = Environment.GetEnvironmentVariable("EVAL_REPORT_PATH") ?? "eval-report.md";
        await File.WriteAllTextAsync(reportPath, markdown);

        Assert.Equal(0, EvalMetrics.ForbiddenCitations(results));
        Assert.True(EvalMetrics.RefusalCorrectness(results) >= 0.8,
            $"Refusal correctness {EvalMetrics.RefusalCorrectness(results):P0} below 80%.");
        Assert.True(EvalMetrics.CitationRecall(results) >= 0.5,
            $"Citation recall {EvalMetrics.CitationRecall(results):P0} below 50%.");
    }

    static (IGroundedAnswerService Service, string Model) BuildAzureService()
    {
        var endpoint = new Uri(Required("AZURE_OPENAI_ENDPOINT"));
        var chatDeployment = Required("AZURE_OPENAI_CHAT_DEPLOYMENT");
        var embeddingDeployment = Required("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

        var client = string.IsNullOrWhiteSpace(key)
            ? AzureOpenAIClientFactory.Create(endpoint, new DefaultAzureCredential())
            : AzureOpenAIClientFactory.Create(endpoint, key);

        var service = new GroundedAnswerService(
            AzureOpenAIClientFactory.ChatClient(client, chatDeployment),
            AzureOpenAIClientFactory.EmbeddingGenerator(client, embeddingDeployment),
            new PgVectorRetriever(DataSourceFactory.Create(Required("FCA_DB"))),
            new PassThroughContentSafety(),
            new RegexPiiRedactor(),
            new InMemoryAuditSink(),
            new GroundedAnswerOptions { RetrievalFloor = 0.35, Model = chatDeployment });

        return (service, chatDeployment);
    }

    static string Required(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Environment variable {name} is required for live evals.");
}
