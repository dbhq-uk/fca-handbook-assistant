using System.Text.Json;
using FcaHandbookAssistant.Core.Domain;

namespace FcaHandbookAssistant.Core.Ai;

/// <summary>
/// Parses the model's JSON answer (see <see cref="PromptTemplates.System"/>) into a
/// <see cref="GroundedAnswer"/>. Shared by the RAG service and the tool-calling agent so both
/// interpret the model contract identically. Returns <c>null</c> on invalid or empty JSON.
/// </summary>
public static class GroundedAnswerJson
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static GroundedAnswer? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<AnswerDto>(text, Options);
            if (dto is null)
            {
                return null;
            }

            var citations = (dto.Citations ?? [])
                .Select(c => new Citation(c.Reference ?? string.Empty, c.Url ?? string.Empty, c.Quote ?? string.Empty))
                .ToArray();

            return new GroundedAnswer(dto.Answer ?? string.Empty, citations, dto.Refused, dto.Reason);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    sealed record AnswerDto(string? Answer, List<CitationDto>? Citations, bool Refused, string? Reason);

    sealed record CitationDto(string? Reference, string? Url, string? Quote);
}
