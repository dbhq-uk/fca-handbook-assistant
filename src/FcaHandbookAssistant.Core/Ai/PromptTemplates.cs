using System.Text;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Ai;

/// <summary>
/// The grounding prompt. The system message constrains the model to answer only from the
/// supplied provisions and to cite each claim; the deterministic <c>GroundingPolicy</c> then
/// enforces it. <see cref="PromptVersion"/> is recorded in the audit log for traceability.
/// </summary>
public static class PromptTemplates
{
    public const string PromptVersion = "2026-07-10.1";

    public const string System =
        """
        You are a compliance assistant answering questions about the FCA Handbook.
        Answer ONLY using the numbered provisions supplied in the user message. Every claim in
        your answer must cite the provision reference it comes from. If the provisions do not
        answer the question, set "refused" to true and briefly say so. Never use outside
        knowledge and never invent a rule or a reference.

        Respond with a single JSON object of this shape and nothing else:
        {
          "answer": "the answer, or empty if refused",
          "citations": [ { "reference": "e.g. PRIN 2.1.1", "url": "the provision url", "quote": "the sentence relied on" } ],
          "refused": false,
          "reason": "why, if refused"
        }
        """;

    public static string BuildUserMessage(string question, IReadOnlyList<RetrievedProvision> provisions)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Question:");
        builder.AppendLine(question);
        builder.AppendLine();
        builder.AppendLine("Provisions you may cite (use the exact reference and url shown):");
        foreach (var retrieved in provisions)
        {
            var provision = retrieved.Provision;
            builder.AppendLine($"- {provision.Reference} | {provision.Url}");
            builder.AppendLine($"  {provision.ChunkText}");
        }

        return builder.ToString();
    }
}
