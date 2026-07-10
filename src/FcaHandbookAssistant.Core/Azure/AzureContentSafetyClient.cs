using Azure.AI.ContentSafety;
using FcaHandbookAssistant.Core.Guardrails;

namespace FcaHandbookAssistant.Core.Azure;

/// <summary>
/// Content Safety backed by Azure AI Content Safety. Text is blocked when any harm category
/// reaches <paramref name="blockAtSeverity"/> (Azure severities are 0-7; 0,2,4,6 by default).
/// The default of 4 (medium) avoids over-blocking legitimate compliance questions that mention
/// sensitive topics such as fraud or money laundering in a regulatory context.
/// </summary>
public sealed class AzureContentSafetyClient : IContentSafetyClient
{
    readonly ContentSafetyClient _client;
    readonly int _blockAtSeverity;

    public AzureContentSafetyClient(ContentSafetyClient client, int blockAtSeverity = 4)
    {
        _client = client;
        _blockAtSeverity = blockAtSeverity;
    }

    public async Task<SafetyVerdict> InspectAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return SafetyVerdict.Safe;
        }

        var response = await _client.AnalyzeTextAsync(new AnalyzeTextOptions(text), cancellationToken);
        var categories = response.Value.CategoriesAnalysis;

        var raw = string.Join(",", categories.Select(c => $"{c.Category}:{c.Severity}"));

        var worst = categories
            .Where(c => c.Severity is not null)
            .OrderByDescending(c => c.Severity!.Value)
            .FirstOrDefault();

        return worst is not null && worst.Severity >= _blockAtSeverity
            ? new SafetyVerdict(Blocked: true, worst.Category.ToString(), raw)
            : new SafetyVerdict(Blocked: false, Category: null, raw);
    }
}
