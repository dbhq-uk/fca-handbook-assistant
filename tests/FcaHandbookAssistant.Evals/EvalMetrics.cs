using FcaHandbookAssistant.Core.Grounding;

namespace FcaHandbookAssistant.Evals;

/// <summary>
/// The regulated-behaviour metrics: refusal correctness (does the assistant refuse exactly when it
/// should), citation recall (are the expected provisions cited), and forbidden citations (did any
/// answer cite a provision it must not - the hallucination signal). References are matched
/// case- and whitespace-insensitively.
/// </summary>
public static class EvalMetrics
{
    public static double RefusalCorrectness(IReadOnlyList<EvalResult> results)
    {
        if (results.Count == 0)
        {
            return 1.0;
        }

        return results.Count(r => r.Outcome.Refused == r.Expectation.ShouldRefuse) / (double)results.Count;
    }

    public static double CitationRecall(IReadOnlyList<EvalResult> results)
    {
        var answerCases = results
            .Where(r => !r.Expectation.ShouldRefuse && r.Expectation.ExpectedCitations.Count > 0)
            .ToList();

        if (answerCases.Count == 0)
        {
            return 1.0;
        }

        return answerCases.Average(r =>
        {
            var cited = r.Outcome.CitedReferences.Select(GroundingPolicy.NormaliseReference).ToHashSet();
            var expected = r.Expectation.ExpectedCitations.Select(GroundingPolicy.NormaliseReference).ToList();
            return expected.Count(cited.Contains) / (double)expected.Count;
        });
    }

    public static int ForbiddenCitations(IReadOnlyList<EvalResult> results) =>
        results.Sum(r =>
        {
            var forbidden = r.Expectation.MustNotCite.Select(GroundingPolicy.NormaliseReference).ToHashSet();
            return r.Outcome.CitedReferences.Count(c => forbidden.Contains(GroundingPolicy.NormaliseReference(c)));
        });
}
