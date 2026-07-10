namespace FcaHandbookAssistant.Core.Guardrails;

/// <summary>
/// Inspects text (user input and model output) for harmful content. The Azure implementation
/// calls Content Safety; a local pass-through is used for dev, tests, and CI.
/// </summary>
public interface IContentSafetyClient
{
    Task<SafetyVerdict> InspectAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// The outcome of a safety inspection: whether the text is <see cref="Blocked"/>, the offending
/// <see cref="Category"/> if so, and a <see cref="Raw"/> summary of category severities for the audit log.
/// </summary>
public sealed record SafetyVerdict(bool Blocked, string? Category, string Raw)
{
    public static SafetyVerdict Safe { get; } = new(false, null, "safe");
}
