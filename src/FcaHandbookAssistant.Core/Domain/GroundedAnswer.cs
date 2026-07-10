namespace FcaHandbookAssistant.Core.Domain;

/// <summary>
/// The outcome of a grounded question: either an answer supported by one or more
/// <see cref="Citations"/>, or a refusal (<see cref="Refused"/> is <c>true</c> with a
/// <see cref="Reason"/>). The grounding policy guarantees that a non-refused answer only
/// carries citations to provisions that were actually retrieved.
/// </summary>
public sealed record GroundedAnswer(
    string Text,
    IReadOnlyList<Citation> Citations,
    bool Refused,
    string? Reason)
{
    /// <summary>Creates a refusal with no answer text and no citations.</summary>
    public static GroundedAnswer Refusal(string reason) =>
        new(string.Empty, Array.Empty<Citation>(), Refused: true, reason);
}
