namespace FcaHandbookAssistant.Core.Ingestion;

/// <summary>
/// Fetches provision pages from handbook.fca.org.uk politely: an identified user agent (set on the
/// injected <see cref="HttpClient"/>), a delay between requests, and only the curated manifest URLs
/// (an allow-list, never a crawl). Returns the cleaned provision text.
/// </summary>
public sealed class HandbookFetcher
{
    readonly HttpClient _httpClient;
    readonly TimeSpan _delayBetweenRequests;

    public HandbookFetcher(HttpClient httpClient, TimeSpan? delayBetweenRequests = null)
    {
        _httpClient = httpClient;
        _delayBetweenRequests = delayBetweenRequests ?? TimeSpan.FromSeconds(1);
    }

    public async Task<string> FetchTextAsync(string url, CancellationToken cancellationToken = default)
    {
        var html = await _httpClient.GetStringAsync(url, cancellationToken);
        await Task.Delay(_delayBetweenRequests, cancellationToken);
        return ProvisionChunker.ToCleanText(html);
    }
}
