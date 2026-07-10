using System.Diagnostics;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Ai;
using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.Core.Guardrails;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Core.Grounding;

/// <summary>
/// The regulated answer pipeline: content-safety and PII checks on input, retrieval, a floor
/// check that refuses weak matches without a model call, a grounded generation, the
/// deterministic <see cref="GroundingPolicy"/> (citations validated against what was retrieved,
/// with authoritative Handbook URLs), a content-safety check on output, and an audit record on
/// every path - answered or refused.
/// </summary>
public sealed class GroundedAnswerService : IGroundedAnswerService
{
    readonly IChatClient _chatClient;
    readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    readonly IRetriever _retriever;
    readonly IContentSafetyClient _safety;
    readonly IPiiRedactor _pii;
    readonly IAuditSink _audit;
    readonly GroundedAnswerOptions _options;

    public GroundedAnswerService(
        IChatClient chatClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IRetriever retriever,
        IContentSafetyClient safety,
        IPiiRedactor pii,
        IAuditSink audit,
        GroundedAnswerOptions options)
    {
        _chatClient = chatClient;
        _embeddings = embeddings;
        _retriever = retriever;
        _safety = safety;
        _pii = pii;
        _audit = audit;
        _options = options;
    }

    public async Task<GroundedAnswer> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var redacted = _pii.Redact(question).Redacted;

        var inputSafety = await _safety.InspectAsync(redacted, cancellationToken);
        if (inputSafety.Blocked)
        {
            return await FinishAsync(
                GroundedAnswer.Refusal("The question was blocked by content safety."),
                redacted, [], inputSafety, null, _options.Model, 0, 0, stopwatch, cancellationToken);
        }

        var queryEmbedding = (await _embeddings.GenerateAsync([redacted], cancellationToken: cancellationToken))[0].Vector;
        var retrieved = await _retriever.RetrieveAsync(queryEmbedding, _options.RetrievalCount, cancellationToken);

        // Refuse weak retrieval before spending a model call.
        if (retrieved.Count == 0 || retrieved.Max(r => r.Score) < _options.RetrievalFloor)
        {
            return await FinishAsync(
                GroundedAnswer.Refusal("Not found in the provisions available."),
                redacted, retrieved, inputSafety, null, _options.Model, 0, 0, stopwatch, cancellationToken);
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, PromptTemplates.System),
            new(ChatRole.User, PromptTemplates.BuildUserMessage(redacted, retrieved)),
        };
        var response = await _chatClient.GetResponseAsync(
            messages, new ChatOptions { ResponseFormat = ChatResponseFormat.Json }, cancellationToken);

        var model = response.ModelId ?? _options.Model;
        var promptTokens = (int)(response.Usage?.InputTokenCount ?? 0);
        var completionTokens = (int)(response.Usage?.OutputTokenCount ?? 0);

        var parsed = GroundedAnswerJson.TryParse(response.Text);
        var answer = parsed is null
            ? GroundedAnswer.Refusal("The model did not return a valid grounded answer.")
            : GroundingPolicy.Apply(retrieved, parsed, _options.RetrievalFloor);

        if (!answer.Refused)
        {
            answer = WithAuthoritativeUrls(answer, retrieved);
        }

        SafetyVerdict? outputSafety = null;
        if (!answer.Refused)
        {
            outputSafety = await _safety.InspectAsync(answer.Text, cancellationToken);
            if (outputSafety.Blocked)
            {
                answer = GroundedAnswer.Refusal("The answer was blocked by content safety.");
            }
        }

        return await FinishAsync(
            answer, redacted, retrieved, inputSafety, outputSafety, model, promptTokens, completionTokens, stopwatch, cancellationToken);
    }

    async Task<GroundedAnswer> FinishAsync(
        GroundedAnswer answer,
        string redactedQuestion,
        IReadOnlyList<RetrievedProvision> retrieved,
        SafetyVerdict? inputSafety,
        SafetyVerdict? outputSafety,
        string model,
        int promptTokens,
        int completionTokens,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var record = new AuditRecord(
            DateTimeOffset.UtcNow,
            redactedQuestion,
            retrieved.Select(r => new RetrievedReference(r.Provision.Reference, r.Score)).ToArray(),
            answer.Citations.Select(c => c.Reference).ToArray(),
            answer.Refused,
            answer.Reason,
            model,
            PromptTemplates.PromptVersion,
            inputSafety?.Raw,
            outputSafety?.Raw,
            stopwatch.ElapsedMilliseconds,
            promptTokens,
            completionTokens);

        await _audit.WriteAsync(record, cancellationToken);
        return answer;
    }

    static GroundedAnswer WithAuthoritativeUrls(GroundedAnswer answer, IReadOnlyList<RetrievedProvision> retrieved)
    {
        var urlByReference = new Dictionary<string, string>();
        foreach (var retrievedProvision in retrieved)
        {
            urlByReference[GroundingPolicy.NormaliseReference(retrievedProvision.Provision.Reference)] =
                retrievedProvision.Provision.Url;
        }

        var citations = answer.Citations
            .Select(c => urlByReference.TryGetValue(GroundingPolicy.NormaliseReference(c.Reference), out var url)
                ? c with { Url = url }
                : c)
            .ToArray();

        return answer with { Citations = citations };
    }
}
