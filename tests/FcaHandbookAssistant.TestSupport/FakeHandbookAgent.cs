using FcaHandbookAssistant.Core.Agent;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.TestSupport;

/// <summary>An <see cref="IHandbookAgent"/> that returns a fixed answer, for tests and cloud-free UI.</summary>
public sealed class FakeHandbookAgent : IHandbookAgent
{
    readonly GroundedAnswer _answer;

    public FakeHandbookAgent(GroundedAnswer answer) => _answer = answer;

    public Task<GroundedAnswer> AskAsync(string question, CancellationToken cancellationToken = default) =>
        Task.FromResult(_answer);
}
