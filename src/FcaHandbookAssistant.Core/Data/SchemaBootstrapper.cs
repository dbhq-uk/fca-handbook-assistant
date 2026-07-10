using System.Reflection;
using Npgsql;

namespace FcaHandbookAssistant.Core.Data;

/// <summary>Applies the database schema (from the embedded <c>schema.sql</c>) idempotently.</summary>
public static class SchemaBootstrapper
{
    const string ResourceName = "FcaHandbookAssistant.Core.Data.schema.sql";

    public static async Task EnsureAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken = default)
    {
        var sql = await ReadSchemaAsync(cancellationToken);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
