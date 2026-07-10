using FcaHandbookAssistant.Core.Agent;
using FcaHandbookAssistant.Core.Grounding;

namespace FcaHandbookAssistant.Api.Endpoints;

/// <summary>The JSON API: ask a grounded question, ask the agent, and a health probe.</summary>
public static class AskEndpoints
{
    public sealed record AskRequest(string Question);

    public static void MapAskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.MapPost("/api/ask", async (AskRequest request, IGroundedAnswerService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.AskAsync(request.Question, cancellationToken)));

        app.MapPost("/api/agent/ask", async (AskRequest request, IHandbookAgent agent, CancellationToken cancellationToken) =>
            Results.Ok(await agent.AskAsync(request.Question, cancellationToken)));
    }
}
