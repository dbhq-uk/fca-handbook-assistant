namespace FcaHandbookAssistant.Core.Guardrails;

/// <summary>
/// A content-safety client that treats all text as safe. Used for local/dev runs without a
/// Content Safety resource; the Azure implementation is used in the cloud.
/// </summary>
public sealed class PassThroughContentSafety : IContentSafetyClient
{
    public Task<SafetyVerdict> InspectAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(SafetyVerdict.Safe);
}
