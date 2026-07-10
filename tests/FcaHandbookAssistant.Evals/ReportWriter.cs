using System.Text;

namespace FcaHandbookAssistant.Evals;

/// <summary>Renders an eval run as a markdown report for CI artifacts and the write-up.</summary>
public static class ReportWriter
{
    public static string BuildMarkdown(IReadOnlyList<EvalResult> results, string model, string promptVersion, DateTimeOffset generatedAt)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# FCA Handbook assistant - eval report");
        builder.AppendLine();
        builder.AppendLine($"- Generated: {generatedAt:u}");
        builder.AppendLine($"- Model: `{model}`");
        builder.AppendLine($"- Prompt version: `{promptVersion}`");
        builder.AppendLine($"- Cases: {results.Count}");
        builder.AppendLine($"- Refusal correctness: {EvalMetrics.RefusalCorrectness(results):P0}");
        builder.AppendLine($"- Citation recall: {EvalMetrics.CitationRecall(results):P0}");
        builder.AppendLine($"- Forbidden (hallucinated) citations: {EvalMetrics.ForbiddenCitations(results)}");
        builder.AppendLine();
        builder.AppendLine("| Question | Expected | Outcome | Cited |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var result in results)
        {
            var expected = result.Expectation.ShouldRefuse
                ? "refuse"
                : string.Join(", ", result.Expectation.ExpectedCitations);
            var outcome = result.Outcome.Refused ? "refused" : "answered";
            var cited = string.Join(", ", result.Outcome.CitedReferences);
            builder.AppendLine($"| {Escape(result.Question)} | {expected} | {outcome} | {cited} |");
        }

        return builder.ToString();
    }

    static string Escape(string value) => value.Replace("|", "\\|");
}
