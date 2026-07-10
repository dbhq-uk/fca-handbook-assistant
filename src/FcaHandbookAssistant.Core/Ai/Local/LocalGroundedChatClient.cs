using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Core.Ai.Local;

/// <summary>
/// A deterministic, offline <see cref="IChatClient"/> that lets the app produce real grounded,
/// cited answers without a cloud model. It reads the provisions block that <see cref="PromptTemplates"/>
/// places in the user message, then answers by citing the top provision - returning the same JSON
/// contract a real model would. Used for local/dev/CI; the Azure model is used in the cloud.
/// </summary>
public sealed partial class LocalGroundedChatClient : IChatClient
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var userText = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        var provisions = ParseProvisions(userText);

        object payload;
        if (provisions.Count == 0)
        {
            payload = new { answer = string.Empty, citations = Array.Empty<object>(), refused = true, reason = "No provisions were supplied." };
        }
        else
        {
            var (reference, url, text) = provisions[0];
            var quote = Snippet(text);
            payload = new
            {
                answer = $"Based on {reference}: {text}",
                citations = new[] { new { reference, url, quote } },
                refused = false,
                reason = (string?)null,
            };
        }

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(payload, Json)))
        {
            ModelId = "local-grounded",
        };
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
        // Nothing to dispose.
    }

    static List<(string Reference, string Url, string Text)> ParseProvisions(string userText)
    {
        var provisions = new List<(string, string, string)>();
        var lines = userText.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var match = ProvisionLineRegex().Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            var text = i + 1 < lines.Length ? lines[i + 1].Trim() : string.Empty;
            provisions.Add((match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim(), text));
        }

        return provisions;
    }

    static string Snippet(string text, int max = 200) =>
        text.Length <= max ? text : text[..max].TrimEnd() + "...";

    // Matches "- PRIN 2.1.1 | https://handbook.fca.org.uk/..." from PromptTemplates.BuildUserMessage.
    [GeneratedRegex(@"^-\s+(.+?)\s+\|\s+(\S+)\s*$")]
    private static partial Regex ProvisionLineRegex();
}
