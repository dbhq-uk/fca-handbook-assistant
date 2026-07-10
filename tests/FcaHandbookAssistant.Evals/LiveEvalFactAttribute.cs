using Xunit;

namespace FcaHandbookAssistant.Evals;

/// <summary>
/// Runs only when <c>RUN_LIVE_EVALS=1</c> (with Azure + FCA_DB configured). The live evals call a
/// real model and are billed, so they are skipped in ordinary CI and run in the deploy session.
/// </summary>
public sealed class LiveEvalFactAttribute : FactAttribute
{
    public LiveEvalFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_LIVE_EVALS") != "1")
        {
            Skip = "Set RUN_LIVE_EVALS=1 (with Azure and FCA_DB configured) to run the live evals.";
        }
    }
}
