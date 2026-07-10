using System.Diagnostics.Metrics;

namespace FcaHandbookAssistant.Core.Observability;

/// <summary>
/// Custom metrics for the regulated behaviour and cost: answers vs refusals (so refusal rate can be
/// charted), citations per answer, and token usage and latency. Emitted on every question and
/// exported to Application Insights via OpenTelemetry when configured.
/// </summary>
public sealed class HandbookMetrics
{
    public const string MeterName = "FcaHandbookAssistant";

    readonly Counter<long> _answers;
    readonly Counter<long> _refusals;
    readonly Histogram<int> _citations;
    readonly Histogram<int> _promptTokens;
    readonly Histogram<int> _completionTokens;
    readonly Histogram<double> _latencyMs;

    public HandbookMetrics(IMeterFactory? meterFactory = null)
    {
        var meter = meterFactory?.Create(MeterName) ?? new Meter(MeterName);
        _answers = meter.CreateCounter<long>("handbook.answers", description: "Grounded answers returned.");
        _refusals = meter.CreateCounter<long>("handbook.refusals", description: "Refusals returned.");
        _citations = meter.CreateHistogram<int>("handbook.citations", description: "Citations per answer.");
        _promptTokens = meter.CreateHistogram<int>("handbook.prompt_tokens", description: "Prompt tokens per question.");
        _completionTokens = meter.CreateHistogram<int>("handbook.completion_tokens", description: "Completion tokens per question.");
        _latencyMs = meter.CreateHistogram<double>("handbook.latency_ms", description: "End-to-end latency per question.");
    }

    public void Record(bool refused, int citationCount, int promptTokens, int completionTokens, double latencyMs)
    {
        if (refused)
        {
            _refusals.Add(1);
        }
        else
        {
            _answers.Add(1);
        }

        _citations.Record(citationCount);
        _promptTokens.Record(promptTokens);
        _completionTokens.Record(completionTokens);
        _latencyMs.Record(latencyMs);
    }
}
