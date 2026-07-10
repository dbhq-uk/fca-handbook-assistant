using System.Reflection;
using Npgsql;

namespace FcaHandbookAssistant.Core.Data;

/// <summary>Applies the database schema (from the embedded <c>schema.sql</c>) idempotently.</summary>
public static class SchemaBootstrapper
{
    const string ResourceName = "FcaHandbookAssistant.Core.Data.schema.sql";

    public static async Task EnsureAsync(NpgsqlDataSource dataSource, int embeddingDimensions = 1536, CancellationToken cancellationToken = default)
    {
        var sql = await ReadSchemaAsync(cancellationToken);
        // The schema is authored at 1536 (text-embedding-3-small); rewrite for other providers
        // (for example Ollama all-minilm at 384). The store dimension must match the generator.
        sql = sql.Replace("vector(1536)", $"vector({embeddingDimensions})", StringComparison.Ordinal);
        await using (var command = dataSource.CreateCommand(sql))
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // On a fresh database the data source loaded its type catalogue before CREATE EXTENSION
        // vector ran, so it cannot map the 'vector' type yet. Reload so pgvector parameters bind.
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ReloadTypesAsync(cancellationToken);
    }

    static async Task<string> ReadSchemaAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(SchemaBootstrapper).GetTypeInfo().Assembly;
        await using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
