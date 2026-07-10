using FcaHandbookAssistant.Core.Ai;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Data;
using FcaHandbookAssistant.Core.Grounding;
using FcaHandbookAssistant.Core.Guardrails;
using FcaHandbookAssistant.Core.Retrieval;
using FcaHandbookAssistant.TestSupport;

namespace FcaHandbookAssistant.Evals;

/// <summary>
/// Measures real semantic retrieval quality against a pgvector store populated with Ollama
/// embeddings. Retrieval is the model under test; the deterministic local chat client cites the
/// top retrieved provision, so this isolates whether semantic retrieval + grounding pick the right
/// provision and refuse out-of-scope questions. Env-gated (see <see cref="SemanticEvalFactAttribute"/>).
/// </summary>
public class SemanticEvals
{
    const double Floor = 0.35;

    [SemanticEvalFact]
    public async Task Semantic_retrieval_grounds_and_refuses_across_the_gold_set()
    {
        var settings = new EmbeddingSettings
        {
            Provider = EmbeddingProvider.Ollama,
            Dimensions = int.Parse(Required("EMBEDDING_DIM")),
            OllamaEndpoint = Required("OLLAMA_ENDPOINT"),
            OllamaModel = Required("OLLAMA_MODEL"),
        };

        await using var dataSource = DataSourceFactory.Create(Required("FCA_DB"));
        var service = new GroundedAnswerService(
            new LocalGroundedChatClient(),
            EmbeddingGeneratorFactory.Create(settings),
            new PgVectorRetriever(dataSource),
            new PassThroughContentSafety(),
            new RegexPiiRedactor(),
            new InMemoryAuditSink(),
            new GroundedAnswerOptions { RetrievalFloor = Floor, Model = $"ollama:{settings.OllamaModel}" });

        var results = new List<EvalResult>();
        foreach (var goldCase in GoldSet.Load())
        {
            var answer = await service.AskAsync(goldCase.Question);
            results.Add(new EvalResult(
                goldCase.Question,
                goldCase.ToExpectation(),
                new EvalOutcome(answer.Refused, answer.Citations.Select(c => c.Reference).ToList())));
        }

        var markdown = ReportWriter.BuildMarkdown(results, $"ollama:{settings.OllamaModel}", "retrieval-only", DateTimeOffset.UtcNow);
        await File.WriteAllTextAsync(Environment.GetEnvironmentVariable("EVAL_REPORT_PATH") ?? "semantic-eval-report.md", markdown);

        Assert.Equal(0, EvalMetrics.ForbiddenCitations(results));
        Assert.True(EvalMetrics.RefusalCorrectness(results) >= 0.85,
            $"Refusal correctness {EvalMetrics.RefusalCorrectness(results):P0} below 85%.");
        Assert.True(EvalMetrics.CitationRecall(results) >= 0.5,
            $"Citation recall {EvalMetrics.CitationRecall(results):P0} below 50%.");
    }

    static string Required(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Environment variable {name} is required for semantic evals.");
}
