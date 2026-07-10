namespace FcaHandbookAssistant.Core.Domain;

/// <summary>
/// A retrievable chunk of the FCA Handbook, identified by its provision reference
/// (for example <c>PRIN 2.1.1</c>). The text is stored for retrieval and grounding;
/// citations always link back to <see cref="Url"/> on handbook.fca.org.uk.
/// </summary>
public sealed record Provision(
    string Reference,
    string Sourcebook,
    string Title,
    string Url,
    string ChunkText,
    string ContentHash);
