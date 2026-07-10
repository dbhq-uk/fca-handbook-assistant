using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FcaHandbookAssistant.Core.Ingestion;

/// <summary>
/// Turns a fetched provision's HTML into clean, storable text, and hashes it for change
/// detection. Block boundaries become spaces so words are not merged; scripts and styles are
/// dropped; entities are decoded and whitespace collapsed.
/// </summary>
public static partial class ProvisionChunker
{
    public static string ToCleanText(string html)
    {
        var text = ScriptStyleRegex().Replace(html, " ");
        text = BlockBoundaryRegex().Replace(text, " ");
        text = TagRegex().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return text;
    }

    public static string ContentHash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex(@"</?(p|div|li|br|h[1-6]|tr|section|article|table)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBoundaryRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
