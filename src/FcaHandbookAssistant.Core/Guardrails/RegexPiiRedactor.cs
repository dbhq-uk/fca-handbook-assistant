using System.Text.RegularExpressions;

namespace FcaHandbookAssistant.Core.Guardrails;

/// <summary>
/// A lightweight, dependency-free PII redactor for dev, tests, and defence in depth. It covers
/// email addresses, UK phone numbers, National Insurance numbers, and long digit runs / sort
/// codes (account numbers). In the cloud this is complemented by Azure AI Language PII detection.
/// Patterns are applied in order so that phone numbers are not also caught as generic numbers.
/// </summary>
public sealed partial class RegexPiiRedactor : IPiiRedactor
{
    static readonly (string Kind, Regex Pattern)[] Patterns =
    [
        ("EMAIL", EmailRegex()),
        ("NINO", NinoRegex()),
        ("PHONE", PhoneRegex()),
        ("NUMBER", NumberRegex()),
    ];

    public RedactionResult Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new RedactionResult(text ?? string.Empty, []);
        }

        var kinds = new List<string>();
        var result = text;

        foreach (var (kind, pattern) in Patterns)
        {
            result = pattern.Replace(result, _ =>
            {
                if (!kinds.Contains(kind))
                {
                    kinds.Add(kind);
                }

                return $"[REDACTED:{kind}]";
            });
        }

        return new RedactionResult(result, kinds);
    }

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b[A-Za-z]{2}\d{6}[A-Za-z]\b")]
    private static partial Regex NinoRegex();

    [GeneratedRegex(@"(?:\+44\s?7\d{3}|\b07\d{3})[\s-]?\d{6}\b")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{8,}\b|\b\d\d-\d\d-\d\d\b")]
    private static partial Regex NumberRegex();
}
