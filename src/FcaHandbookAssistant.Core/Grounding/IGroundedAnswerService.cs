using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Grounding;

/// <summary>Answers a question with a grounded, cited answer or a refusal, and audits every call.</summary>
public interface IGroundedAnswerService
{
    Task<GroundedAnswer> AskAsync(string question, CancellationToken cancellationToken = default);
}

/// <summary>Tunables for the grounded answer pipeline.</summary>
public sealed class GroundedAnswerOptions
{
    /// <summary>How many provisions to retrieve.</summary>
    public int RetrievalCount { get; init; } = 5;

    /// <summary>The minimum top similarity score to attempt an answer; below it, refuse.</summary>
    public double RetrievalFloor { get; init; } = 0.3;

    /// <summary>Model name recorded in the audit log when the response does not report one.</summary>
    public string Model { get; init; } = "unknown";
}
