using FcaHandbookAssistant.Core.Domain;
using FcaHandbookAssistant.Core.Grounding;

namespace FcaHandbookAssistant.Core.Agent;

/// <summary>
/// A local <see cref="IHandbookAgent"/> that answers via the grounded RAG service. Tool-calling
/// needs a real model, so cloud-free runs fall back to grounded retrieval; the Foundry agent is
/// used in the cloud.
/// </summary>
public sealed class GroundedAnswerAgentAdapter : IHandbookAgent
{
    readonly IGroundedAnswerService _service;

    public GroundedAnswerAgentAdapter(IGroundedAnswerService service) => _service = service;

    public Task<GroundedAnswer> AskAsync(string question, CancellationToken cancellationToken = default) =>
        _service.AskAsync(question, cancellationToken);
}
