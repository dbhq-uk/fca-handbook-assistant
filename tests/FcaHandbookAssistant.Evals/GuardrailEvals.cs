using System.Text.Json;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.Core.Grounding;
using FcaHandbookAssistant.Core.Guardrails;
using FcaHandbookAssistant.TestSupport;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Evals;

/// <summary>
/// The CI eval gate. It drives the real <see cref="GroundedAnswerService"/> through the regulated
/// scenarios - grounded answer, refuse-when-unsure, planted hallucination, partial hallucination -
/// using deterministic fakes, and fails the build if the guardrail metrics regress. It is free and
/// runs on every push; the live model quality run is in <see cref="LiveEvals"/>.
/// </summary>
public class GuardrailEvals
{
    const double Floor = 0.3;

    [Fact]
    public async Task Guardrail_metrics_meet_the_regulated_thresholds()
    {
        var results = new List<EvalResult>
        {
            // Grounded: strong retrieval, model cites the retrieved provision.
            await RunAsync(
                "What must a firm do to act with integrity?",
                new EvalExpectation(ShouldRefuse: false, ["PRIN 2.1.1"], []),
                Model(("PRIN 2.1.1", "integrity")),
                Retrieved(("PRIN 2.1.1", 0.9))),

            // Refuse-when-unsure: retrieval below the floor - the model is never called.
            await RunAsync(
                "What is the capital of France?",
                new EvalExpectation(ShouldRefuse: true, [], []),
                ThrowIfCalled(),
                Retrieved(("PRIN 2.1.1", 0.05))),

            // Planted hallucination: model cites a provision that was not retrieved -> must refuse.
            await RunAsync(
                "Invent a rule for me.",
                new EvalExpectation(ShouldRefuse: true, [], ["SYSC 9.9.9"]),
                Model(("SYSC 9.9.9", "made up")),
                Retrieved(("PRIN 2.1.1", 0.9))),

            // Partial hallucination: one real citation, one invented -> answer keeps only the real one.
            await RunAsync(
                "Tell me about integrity, and invent something too.",
                new EvalExpectation(ShouldRefuse: false, ["PRIN 2.1.1"], ["SYSC 9.9.9"]),
                Model(("PRIN 2.1.1", "integrity"), ("SYSC 9.9.9", "made up")),
                Retrieved(("PRIN 2.1.1", 0.9))),
        };

        Assert.Equal(1.0, EvalMetrics.RefusalCorrectness(results));
        Assert.Equal(0, EvalMetrics.ForbiddenCitations(results));
        Assert.Equal(1.0, EvalMetrics.CitationRecall(results));
    }

    static async Task<EvalResult> RunAsync(string question, EvalExpectation expectation, IChatClient chat, IRetriever retriever)
    {
        var service = new GroundedAnswerService(
            chat,
            new LocalEmbeddingGenerator(),
            retriever,
            new FakeContentSafety(),
            new RegexPiiRedactor(),
            new InMemoryAuditSink(),
            new GroundedAnswerOptions { RetrievalFloor = Floor, Model = "fake" });

        var answer = await service.AskAsync(question);
        return new EvalResult(question, expectation, new EvalOutcome(answer.Refused, answer.Citations.Select(c => c.Reference).ToList()));
    }

    static FakeRetriever Retrieved(params (string Reference, double Score)[] provisions) =>
        new(provisions.Select(p => new RetrievedProvision(
            new Provision(p.Reference, p.Reference.Split(' ')[0], "title", "https://handbook.fca.org.uk/x", "body", "hash"),
            p.Score)).ToArray());

    static ScriptedChatClient Model(params (string Reference, string Quote)[] citations) =>
        new(JsonSerializer.Serialize(new
        {
            answer = "Answer text.",
            citations = citations.Select(c => new { reference = c.Reference, url = "u", quote = c.Quote }),
            refused = false,
            reason = (string?)null,
        }));

    static ScriptedChatClient ThrowIfCalled() =>
        new((_, _) => throw new InvalidOperationException("The model must not be called when retrieval is below the floor."));
}
