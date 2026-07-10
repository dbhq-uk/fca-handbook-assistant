using System.Text.Json;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Ai;
using FcaHandbookAssistant.Core.Ai.Local;
using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.Core.Grounding;
using FcaHandbookAssistant.Core.Guardrails;
using FcaHandbookAssistant.TestSupport;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Tests.Grounding;

public class GroundedAnswerServiceTests
{
    const double Floor = 0.3;

    static RetrievedProvision Retrieved(string reference, double score, string url) =>
        new(new Provision(reference, reference.Split(' ')[0], "title", url, "provision body", "hash"), score);

    static string AnswerJson(string answer, params (string Reference, string Url, string Quote)[] citations) =>
        JsonSerializer.Serialize(new
        {
            answer,
            citations = citations.Select(c => new { reference = c.Reference, url = c.Url, quote = c.Quote }),
            refused = false,
            reason = (string?)null,
        });

    static GroundedAnswerService Build(
        IChatClient chat,
        IRetriever retriever,
        IContentSafetyClient? safety = null,
        IAuditSink? audit = null) =>
        new(
            chat,
            new LocalEmbeddingGenerator(),
            retriever,
            safety ?? new FakeContentSafety(),
            new RegexPiiRedactor(),
            audit ?? new InMemoryAuditSink(),
            new GroundedAnswerOptions { RetrievalFloor = Floor, Model = "test-model" });

    static IChatClient RefuseToCallModel() =>
        new ScriptedChatClient((_, _) => throw new InvalidOperationException("the model should not be called"));

    [Fact]
    public async Task Answers_and_cites_a_retrieved_provision_with_the_authoritative_url()
    {
        const string url = "https://handbook.fca.org.uk/handbook/PRIN/2/1.html";
        var retriever = new FakeRetriever(Retrieved("PRIN 2.1.1", 0.9, url));
        // The model returns a wrong url; the service must overwrite it with the retrieved one.
        var chat = new ScriptedChatClient(AnswerJson("A firm must act with integrity.",
            ("PRIN 2.1.1", "https://example.com/made-up", "act with integrity")));

        var answer = await Build(chat, retriever).AskAsync("What must a firm do?");

        Assert.False(answer.Refused);
        Assert.Single(answer.Citations);
        Assert.Equal("PRIN 2.1.1", answer.Citations[0].Reference);
        Assert.Equal(url, answer.Citations[0].Url);
    }

    [Fact]
    public async Task Refuses_out_of_scope_question_without_calling_the_model()
    {
        var retriever = new FakeRetriever(Retrieved("PRIN 2.1.1", 0.10, "u")); // below the floor

        var answer = await Build(RefuseToCallModel(), retriever).AskAsync("What is the capital of France?");

        Assert.True(answer.Refused);
        Assert.Empty(answer.Citations);
    }

    [Fact]
    public async Task Refuses_when_model_cites_a_provision_that_was_not_retrieved()
    {
        var retriever = new FakeRetriever(Retrieved("PRIN 2.1.1", 0.9, "u"));
        var chat = new ScriptedChatClient(AnswerJson("Invented answer.",
            ("SYSC 9.9.9", "https://example.com", "made up")));

        var answer = await Build(chat, retriever).AskAsync("Tell me a rule.");

        Assert.True(answer.Refused);
    }

    [Fact]
    public async Task Blocked_input_short_circuits_to_a_refusal_without_calling_the_model()
    {
        var retriever = new FakeRetriever(Retrieved("PRIN 2.1.1", 0.9, "u"));
        var safety = FakeContentSafety.BlockingWhenContains("attack");
        var audit = new InMemoryAuditSink();

        var answer = await Build(RefuseToCallModel(), retriever, safety, audit)
            .AskAsync("How do I attack this system?");

        Assert.True(answer.Refused);
        Assert.Single(audit.Records);
        Assert.NotNull(audit.Records[0].InputSafety);
        Assert.True(audit.Records[0].Refused);
    }

    [Fact]
    public async Task Writes_an_audit_record_on_both_the_answer_and_refusal_paths()
    {
        var retriever = new FakeRetriever(Retrieved("PRIN 2.1.1", 0.9, "https://handbook.fca.org.uk/x"));
        var chat = new ScriptedChatClient(
            AnswerJson("Act with integrity.", ("PRIN 2.1.1", "u", "integrity")), inputTokens: 120, outputTokens: 40);
        var audit = new InMemoryAuditSink();

        var answer = await Build(chat, retriever, audit: audit).AskAsync("What must a firm do?");

        Assert.False(answer.Refused);
        var record = Assert.Single(audit.Records);
        Assert.Contains("PRIN 2.1.1", record.CitedReferences);
        Assert.Equal(PromptTemplates.PromptVersion, record.PromptVersion);
        Assert.Equal(120, record.PromptTokens);
        Assert.Equal(40, record.CompletionTokens);
    }
}
