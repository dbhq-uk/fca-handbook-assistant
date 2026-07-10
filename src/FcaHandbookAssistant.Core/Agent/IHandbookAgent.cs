using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Agent;

/// <summary>
/// A tool-calling agent that answers a question by looking up and cross-referencing provisions,
/// returning a grounded answer or a refusal - subject to the same grounding discipline as the
/// RAG pipeline.
/// </summary>
public interface IHandbookAgent
{
    Task<GroundedAnswer> AskAsync(string question, CancellationToken cancellationToken = default);
}
