namespace FcaHandbookAssistant.Evals;

/// <summary>What a gold-set case expects: whether to refuse, and which references to cite (or not).</summary>
public sealed record EvalExpectation(
    bool ShouldRefuse,
    IReadOnlyList<string> ExpectedCitations,
    IReadOnlyList<string> MustNotCite);

/// <summary>What actually happened: refused or not, and the references cited.</summary>
public sealed record EvalOutcome(bool Refused, IReadOnlyList<string> CitedReferences);

/// <summary>A single evaluated case.</summary>
public sealed record EvalResult(string Question, EvalExpectation Expectation, EvalOutcome Outcome);
