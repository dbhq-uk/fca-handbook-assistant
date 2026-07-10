namespace FcaHandbookAssistant.Core.Domain;

/// <summary>A provision reference and the score at which it was retrieved, for the audit log.</summary>
public sealed record RetrievedReference(string Reference, double Score);

/// <summary>
/// The per-answer traceability record persisted for every question, whether answered or
/// refused. The <see cref="Question"/> is stored PII-redacted. This is the regulated-grade
/// audit trail: what was retrieved, what was cited, and the model/prompt/safety context.
/// </summary>
public sealed record AuditRecord(
    DateTimeOffset Timestamp,
    string Question,
    IReadOnlyList<RetrievedReference> Retrieved,
    IReadOnlyList<string> CitedReferences,
    bool Refused,
    string? Reason,
    string Model,
    string PromptVersion,
    string? InputSafety,
    string? OutputSafety,
    long LatencyMs,
    int PromptTokens,
    int CompletionTokens);
