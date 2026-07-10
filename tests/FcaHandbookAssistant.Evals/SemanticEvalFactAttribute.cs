using Xunit;

namespace FcaHandbookAssistant.Evals;

/// <summary>
/// Runs only when <c>RUN_SEMANTIC_EVALS=1</c>. The semantic evals measure real retrieval quality
/// against a pgvector store populated with Ollama embeddings - free, local, no cloud - so CI can
/// gate citation accuracy and refusal on a real embedding model.
/// </summary>
public sealed class SemanticEvalFactAttribute : FactAttribute
{
    public SemanticEvalFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_SEMANTIC_EVALS") != "1")
        {
            Skip = "Set RUN_SEMANTIC_EVALS=1 (with FCA_DB and Ollama) to run the semantic evals.";
        }
    }
}
