using System.Text.Json;

namespace FcaHandbookAssistant.Evals;

/// <summary>A gold-set case as authored in <c>data/gold-set.json</c>.</summary>
public sealed record GoldCase(string Question, string Expect, string[]? ExpectedCitations, string[]? MustNotCite)
{
    public EvalExpectation ToExpectation() => new(
        string.Equals(Expect, "refuse", StringComparison.OrdinalIgnoreCase),
        ExpectedCitations ?? [],
        MustNotCite ?? []);
}

public static class GoldSet
{
    public static IReadOnlyList<GoldCase> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "gold-set.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GoldCase[]>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The gold set could not be read.");
    }
}
