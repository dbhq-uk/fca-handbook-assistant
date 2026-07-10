using FcaHandbookAssistant.Core.Guardrails;

namespace FcaHandbookAssistant.TestSupport;

/// <summary>A configurable <see cref="IContentSafetyClient"/> for tests: safe by default.</summary>
public sealed class FakeContentSafety : IContentSafetyClient
{
    readonly Func<string, SafetyVerdict> _inspect;

    public FakeContentSafety() : this(_ => SafetyVerdict.Safe)
    {
    }

    public FakeContentSafety(Func<string, SafetyVerdict> inspect) => _inspect = inspect;

    /// <summary>A safety client that blocks any text containing <paramref name="phrase"/>.</summary>
    public static FakeContentSafety BlockingWhenContains(string phrase) =>
        new(text => text.Contains(phrase, StringComparison.OrdinalIgnoreCase)
            ? new SafetyVerdict(Blocked: true, "Test", $"matched:{phrase}")
            : SafetyVerdict.Safe);

    public Task<SafetyVerdict> InspectAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(_inspect(text));
}
