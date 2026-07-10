using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.TestSupport;

/// <summary>
/// An <see cref="IChatClient"/> whose response is scripted, so tests control exactly what "the
/// model" says - including a planted hallucination (a citation to a provision that was never
/// retrieved) that the grounding policy must reject.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    readonly Func<IEnumerable<ChatMessage>, ChatOptions?, ChatResponse> _responder;

    public ScriptedChatClient(string responseText, int inputTokens = 0, int outputTokens = 0)
        : this((_, _) => BuildResponse(responseText, inputTokens, outputTokens))
    {
    }

    public ScriptedChatClient(Func<IEnumerable<ChatMessage>, ChatOptions?, ChatResponse> responder) =>
        _responder = responder;

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_responder(messages, options));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = _responder(messages, options);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
        // Nothing to dispose.
    }

    static ChatResponse BuildResponse(string text, int inputTokens, int outputTokens)
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
        if (inputTokens > 0 || outputTokens > 0)
        {
            response.Usage = new UsageDetails { InputTokenCount = inputTokens, OutputTokenCount = outputTokens };
        }

        return response;
    }
}
