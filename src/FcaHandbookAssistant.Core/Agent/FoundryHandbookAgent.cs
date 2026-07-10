using System.Diagnostics;
using Azure.AI.Projects;
using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Ai;
using FcaHandbookAssistant.Core.Domain;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FcaHandbookAssistant.Core.Agent;

/// <summary>
/// The code-first Microsoft Agent Framework agent over an Azure AI Foundry project. It exposes the
/// three handbook tools as function tools, runs the Foundry Responses agent, then applies the same
/// grounding discipline (citations must resolve to a real provision) and writes an audit record.
/// The server-managed Foundry Agent Service is a documented alternative (see the README).
/// </summary>
public sealed class FoundryHandbookAgent : IHandbookAgent
{
    const string AgentPromptVersion = "agent-2026-07-10.1";

    const string Instructions =
        """
        You are a compliance assistant for the FCA Handbook. Use the tools to look up provisions
        (lookup_rule), find related provisions (find_related_rules), and map scenarios to sections
        (check_scenario). Answer only from what the tools return, and cite each claim with its
        provision reference. If the tools do not support an answer, refuse. Respond with a single
        JSON object: { "answer", "citations": [ { "reference", "url", "quote" } ], "refused", "reason" }.
        """;

    readonly AIAgent _agent;
    readonly IProvisionStore _store;
    readonly IAuditSink _audit;
    readonly string _model;

    public FoundryHandbookAgent(
        AIProjectClient projectClient,
        string deploymentName,
        HandbookTools tools,
        IProvisionStore store,
        IAuditSink audit)
    {
        _store = store;
        _audit = audit;
        _model = deploymentName;
        _agent = BuildAgent(projectClient, deploymentName, tools);
    }

    static AIAgent BuildAgent(AIProjectClient projectClient, string deploymentName, HandbookTools tools)
    {
        IList<AITool> aiTools =
        [
            AIFunctionFactory.Create(tools.LookupRuleAsync),
            AIFunctionFactory.Create(tools.FindRelatedRulesAsync),
            AIFunctionFactory.Create(tools.CheckScenarioAsync),
        ];

        var options = new ChatClientAgentOptions
        {
            Name = "fca-handbook-agent",
            ChatOptions = new ChatOptions
            {
                Instructions = Instructions,
                Tools = aiTools,
                ModelId = deploymentName,
                ResponseFormat = ChatResponseFormat.Json,
            },
        };

        return projectClient.AsAIAgent(options);
    }

    public async Task<GroundedAnswer> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await _agent.RunAsync([new ChatMessage(ChatRole.User, question)], null, null, cancellationToken);

        var parsed = GroundedAnswerJson.TryParse(response.Text);
        var answer = parsed is null
            ? GroundedAnswer.Refusal("The agent did not return a valid grounded answer.")
            : await AgentGrounding.ResolveAgainstStoreAsync(_store, parsed, cancellationToken);

        var record = new AuditRecord(
            DateTimeOffset.UtcNow,
            question,
            [],
            answer.Citations.Select(c => c.Reference).ToArray(),
            answer.Refused,
            answer.Reason,
            _model,
            AgentPromptVersion,
            InputSafety: null,
            OutputSafety: null,
            stopwatch.ElapsedMilliseconds,
            PromptTokens: 0,
            CompletionTokens: 0);
        await _audit.WriteAsync(record, cancellationToken);

        return answer;
    }
}
