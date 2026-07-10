using FcaHandbookAssistant.Core.Abstractions;
using FcaHandbookAssistant.Core.Domain;
using Npgsql;
using Pgvector;

namespace FcaHandbookAssistant.Core.Retrieval;

/// <summary>Stores provisions and embeddings in Postgres with pgvector.</summary>
public sealed class PgVectorProvisionStore : IProvisionStore
{
    readonly NpgsqlDataSource _dataSource;

    public PgVectorProvisionStore(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task UpsertAsync(Provision provision, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO provisions (reference, sourcebook, title, url, chunk_text, content_hash, embedding)
            VALUES (@reference, @sourcebook, @title, @url, @chunk_text, @content_hash, @embedding)
            ON CONFLICT (reference) DO UPDATE SET
                sourcebook = EXCLUDED.sourcebook,
                title = EXCLUDED.title,
                url = EXCLUDED.url,
                chunk_text = EXCLUDED.chunk_text,
                content_hash = EXCLUDED.content_hash,
                embedding = EXCLUDED.embedding,
                ingested_at = now();
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("reference", provision.Reference);
        command.Parameters.AddWithValue("sourcebook", provision.Sourcebook);
        command.Parameters.AddWithValue("title", provision.Title);
        command.Parameters.AddWithValue("url", provision.Url);
        command.Parameters.AddWithValue("chunk_text", provision.ChunkText);
        command.Parameters.AddWithValue("content_hash", provision.ContentHash);
        command.Parameters.AddWithValue("embedding", new Vector(embedding));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Provision?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT reference, sourcebook, title, url, chunk_text, content_hash
            FROM provisions
            WHERE reference = @reference;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("reference", reference);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadProvision(reader);
    }

    internal static Provision ReadProvision(NpgsqlDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5));
}
