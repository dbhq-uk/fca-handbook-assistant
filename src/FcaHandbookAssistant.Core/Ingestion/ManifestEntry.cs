namespace FcaHandbookAssistant.Core.Ingestion;

/// <summary>
/// One curated provision to ingest: its reference, sourcebook, title, the authoritative Handbook
/// URL to cite and link back to, and a concise factual <see cref="Summary"/> of what the provision
/// requires. The summary is our own wording (not the Handbook's verbatim text), so the repo never
/// redistributes Crown/FCA copyright material; answers always link to <see cref="Url"/> for the
/// authoritative text. The HTTP fetcher/chunker remain the seam for a future content-API connector.
/// </summary>
public sealed record ManifestEntry(string Reference, string Sourcebook, string Title, string Url, string Summary);
