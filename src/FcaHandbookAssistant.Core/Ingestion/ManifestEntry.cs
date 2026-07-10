namespace FcaHandbookAssistant.Core.Ingestion;

/// <summary>
/// One curated provision to ingest: its reference, sourcebook, title, and the Handbook URL to
/// fetch and link back to. The repo stores only this manifest and code - never the Handbook text.
/// </summary>
public sealed record ManifestEntry(string Reference, string Sourcebook, string Title, string Url);
