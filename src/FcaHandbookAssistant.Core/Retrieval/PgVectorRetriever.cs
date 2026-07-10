using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;
using Pgvector;

namespace FcaHandbookAssistant.Core.Retrieval;

/// <summary>
/// Retrieves provisions by cosine similarity using pgvector's <c>&lt;=&gt;</c> operator and the
/// HNSW index. The returned score is <c>1 - cosine_distance</c>, so 1.0 is an exact match.
/// </summary>
public sealed class PgVectorRetriever : IRetriever
{
    readonly Npgsql.NpgsqlDataSource _dataSource;

    public PgVectorRetriever(Npgsql.NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<RetrievedProvision>> RetrieveAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int k,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT reference, sourcebook, title, url, chunk_text, content_hash,
                   1 - (embedding <=> @query) AS score
            FROM provisions
            ORDER BY embedding <=> @query
            LIMIT @k;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("query", new Vector(queryEmbedding));
        command.Parameters.AddWithValue("k", k);

        var results = new List<RetrievedProvision>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var provision = PgVectorProvisionStore.ReadProvision(reader);
            var score = reader.GetDouble(6);
            results.Add(new RetrievedProvision(provision, score));
        }

        return results;
    }
}
