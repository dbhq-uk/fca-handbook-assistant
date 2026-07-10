namespace FcaHandbookAssistant.Evals;

public class EvalMetricsTests
{
    static EvalResult Case(bool shouldRefuse, string[] expected, string[] mustNot, bool refused, string[] cited) =>
        new("q", new EvalExpectation(shouldRefuse, expected, mustNot), new EvalOutcome(refused, cited));

    [Fact]
    public void RefusalCorrectness_scores_matching_decisions()
    {
        var results = new[]
        {
            Case(shouldRefuse: true, [], [], refused: true, []),   // correct refusal
            Case(shouldRefuse: false, ["PRIN 2.1.1"], [], refused: false, ["PRIN 2.1.1"]), // correct answer
            Case(shouldRefuse: true, [], [], refused: false, ["X 1.1"]), // wrong: should have refused
        };

        Assert.Equal(2.0 / 3.0, EvalMetrics.RefusalCorrectness(results), precision: 5);
    }

    [Fact]
    public void CitationRecall_measures_expected_references_present()
    {
        var results = new[]
        {
            Case(false, ["PRIN 2.1.1", "SYSC 4.1.1"], [], false, ["prin 2.1.1"]), // 1 of 2 (case-insensitive)
        };

        Assert.Equal(0.5, EvalMetrics.CitationRecall(results), precision: 5);
    }

    [Fact]
    public void ForbiddenCitations_counts_hallucinated_references()
    {
        var results = new[]
        {
            Case(false, ["PRIN 2.1.1"], ["SYSC 9.9.9"], false, ["PRIN 2.1.1", "SYSC 9.9.9"]),
        };

        Assert.Equal(1, EvalMetrics.ForbiddenCitations(results));
    }
}
