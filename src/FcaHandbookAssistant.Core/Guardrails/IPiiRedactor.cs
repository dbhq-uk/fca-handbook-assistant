namespace FcaHandbookAssistant.Core.Guardrails;

/// <summary>
/// Redacts personally identifiable information from user-entered text before it is logged,
/// persisted to the audit trail, or sent to the model.
/// </summary>
public interface IPiiRedactor
{
    RedactionResult Redact(string text);
}

/// <summary>The redacted text and the distinct kinds of PII that were found (for example EMAIL, PHONE).</summary>
public sealed record RedactionResult(string Redacted, IReadOnlyList<string> Kinds);
